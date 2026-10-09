/* SPDX-License-Identifier: GPL-2.0-or-later
 * Borderless client-area capture for Windows 10. Independent of WGC, so it
 * does not create a GraphicsCaptureSession or its yellow window outline.
 */
#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <process.h>
#include <obs-module.h>
#include <util/platform.h>

OBS_DECLARE_MODULE()
MODULE_EXPORT const char *obs_module_description(void) { return "EVA borderless window capture"; }

struct eva_capture {
 obs_source_t *source;
 HANDLE stop, thread;
 CRITICAL_SECTION lock;
 HWND window;
 DWORD pid;
 int fps;
 HDC dc;
 HBITMAP bitmap;
 HGDIOBJ previous;
 uint8_t *pixels;
 int width, height;
};

static void free_bitmap(struct eva_capture *c)
{
 if (c->dc && c->previous) SelectObject(c->dc, c->previous);
 if (c->bitmap) DeleteObject(c->bitmap);
 if (c->dc) DeleteDC(c->dc);
 c->bitmap=NULL; c->dc=NULL; c->pixels=NULL; c->previous=NULL;
 c->width=c->height=0;
}

static bool resize_bitmap(struct eva_capture *c, int w, int h)
{
 if (c->width==w && c->height==h && c->bitmap) return true;
 free_bitmap(c);
 if (w<1 || h<1 || w>8192 || h>4320 || (int64_t)w*h>33554432) return false;
 BITMAPINFO info={0};
 info.bmiHeader.biSize=sizeof(BITMAPINFOHEADER);
 info.bmiHeader.biWidth=w; info.bmiHeader.biHeight=-h;
 info.bmiHeader.biPlanes=1; info.bmiHeader.biBitCount=32;
 info.bmiHeader.biCompression=BI_RGB;
 c->dc=CreateCompatibleDC(NULL);
 if (!c->dc) return false;
 c->bitmap=CreateDIBSection(c->dc,&info,DIB_RGB_COLORS,(void **)&c->pixels,NULL,0);
 if (!c->bitmap) { free_bitmap(c); return false; }
 c->previous=SelectObject(c->dc,c->bitmap); c->width=w; c->height=h;
 return true;
}

static unsigned __stdcall capture_thread(void *data)
{
 struct eva_capture *c=data;
 // PrintWindow can synchronously call the target application. It must never
 // run on OBS's render or UI thread. Parent EVA owns OBS in a kill-on-close job.
 while (WaitForSingleObject(c->stop,0)==WAIT_TIMEOUT) {
  uint64_t started=os_gettime_ns();
  EnterCriticalSection(&c->lock);
  HWND hwnd=c->window; DWORD expected_pid=c->pid; int fps=c->fps;
  LeaveCriticalSection(&c->lock);
  RECT rect; DWORD actual_pid=0; DWORD_PTR ping=0;
  bool usable=hwnd && obs_source_showing(c->source) && IsWindow(hwnd) && !IsIconic(hwnd);
  if (usable) {
   GetWindowThreadProcessId(hwnd,&actual_pid);
   usable=actual_pid==expected_pid && GetClientRect(hwnd,&rect) &&
    !IsHungAppWindow(hwnd) && SendMessageTimeoutW(hwnd,WM_NULL,0,0,SMTO_ABORTIFHUNG|SMTO_BLOCK,100,&ping);
  }
  if (usable && resize_bitmap(c,rect.right-rect.left,rect.bottom-rect.top)) {
   // PW_CLIENTONLY | PW_RENDERFULLCONTENT (Win 8.1+, used by WebRTC's capturer).
   if (PrintWindow(hwnd,c->dc,3)) {
    GdiFlush();
    struct obs_source_frame frame={0};
    frame.data[0]=c->pixels; frame.linesize[0]=(uint32_t)c->width*4;
    frame.width=c->width; frame.height=c->height; frame.format=VIDEO_FORMAT_BGRX;
    frame.timestamp=os_gettime_ns(); frame.full_range=true;
    obs_source_output_video(c->source,&frame);
   } else obs_source_output_video(c->source,NULL);
  } else {
   obs_source_output_video(c->source,NULL);
   free_bitmap(c);
  }
  uint64_t elapsed=os_gettime_ns()-started;
  uint64_t interval=1000000000ULL/(usable?fps:5);
  DWORD wait=elapsed<interval?(DWORD)((interval-elapsed)/1000000ULL):1;
  if (WaitForSingleObject(c->stop,wait)==WAIT_OBJECT_0) break;
 }
 free_bitmap(c);
 return 0;
}

static const char *capture_name(void *unused) { UNUSED_PARAMETER(unused); return "EVA Window (borderless)"; }
static void capture_update(void *data, obs_data_t *settings)
{
 struct eva_capture *c=data;
 EnterCriticalSection(&c->lock);
 c->window=(HWND)(uintptr_t)obs_data_get_int(settings,"hwnd");
 c->pid=(DWORD)obs_data_get_int(settings,"process_id");
 c->fps=(int)obs_data_get_int(settings,"fps");
 if (c->fps<1 || c->fps>120) c->fps=30;
 LeaveCriticalSection(&c->lock);
}
static void *capture_create(obs_data_t *settings, obs_source_t *source)
{
 struct eva_capture *c=bzalloc(sizeof(*c)); c->source=source;
 InitializeCriticalSection(&c->lock); c->stop=CreateEventW(NULL,TRUE,FALSE,NULL);
 if (!c->stop) { DeleteCriticalSection(&c->lock); bfree(c); return NULL; }
 capture_update(c,settings);
 obs_source_set_async_unbuffered(source,true);
 obs_source_set_async_decoupled(source,true);
 c->thread=(HANDLE)_beginthreadex(NULL,0,capture_thread,c,0,NULL);
 if (!c->thread) { CloseHandle(c->stop); DeleteCriticalSection(&c->lock); bfree(c); return NULL; }
 return c;
}
static void capture_destroy(void *data)
{
 struct eva_capture *c=data; SetEvent(c->stop);
 WaitForSingleObject(c->thread,INFINITE);
 CloseHandle(c->thread); CloseHandle(c->stop); DeleteCriticalSection(&c->lock); bfree(c);
}
static void capture_defaults(obs_data_t *settings) { obs_data_set_default_int(settings,"fps",30); }
bool obs_module_load(void)
{
 struct obs_source_info info={0};
 info.id="eva_print_capture"; info.type=OBS_SOURCE_TYPE_INPUT;
 info.output_flags=OBS_SOURCE_ASYNC_VIDEO|OBS_SOURCE_DO_NOT_DUPLICATE;
 info.get_name=capture_name; info.create=capture_create; info.destroy=capture_destroy;
 info.update=capture_update; info.get_defaults=capture_defaults;
 obs_register_source(&info);
 return true;
}
