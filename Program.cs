using SDL2;
using SkiaSharp;
using SkiaSharp.Views.GlesInterop;
using System;
using System.Diagnostics;

namespace ConsoleApp2
{
    class Program : IDisposable
    {
        public Window Window { get; private set; }

        Program()
        {
            SDL.SDL_SetHint(SDL.SDL_HINT_WINDOWS_DISABLE_THREAD_NAMING, "1");
            if (SDL.SDL_Init(SDL.SDL_INIT_VIDEO|0x4000) != 0)
            {
                throw new Exception("Unable to init SDL");
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Window?.Dispose();
                }

                SDL.SDL_Quit();
                disposedValue = true;
            }
        }

        ~Program()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

        void CreateWindow()
        {
            Debug.Assert(Window == null);
            Window = new Window("Hello World", 640, 480);
        }

        static void Main(string[] args)
        {
            using (var program = new Program())
            {

                program.CreateWindow();

                while (!program.Window.Closed)
                {
                    program.Window.PollEvents();

                    program.Window.Canvas.Clear(SKColors.AliceBlue);

                    var paint = new SKPaint
                    {
                        Color = SKColors.Black,
                        IsAntialias = true,
                        Style = SKPaintStyle.Fill,
                        TextAlign = SKTextAlign.Center,
                        TextSize = 24
                    };

                    var point = new SKPoint(program.Window.Width / 2, program.Window.Height / 2 + 12);

                    program.Window.Canvas.DrawText("Hello World!", point, paint);
                    program.Window.Canvas.Flush();

                    program.Window.Swap();
                }

            }
        }

    }

    class Window : IDisposable
    {
        const SDL.SDL_WindowFlags WINDOW_FLAGS = SDL.SDL_WindowFlags.SDL_WINDOW_SHOWN | SDL.SDL_WindowFlags.SDL_WINDOW_RESIZABLE
                    | SDL.SDL_WindowFlags.SDL_WINDOW_ALLOW_HIGHDPI | SDL.SDL_WindowFlags.SDL_WINDOW_OPENGL;

        private readonly IntPtr _window;

        private readonly IntPtr _glContext;

        private readonly SKSurface _surface;

        public SKCanvas Canvas {  get
            {
                return _surface.Canvas;
            }
        }

        public bool Closed { get; private set; }

        public int Width { get; private set; }

        public int Height { get; private set; }

        public Window(string title, int w, int h)
        {
            const int x = SDL.SDL_WINDOWPOS_CENTERED;
            const int y = SDL.SDL_WINDOWPOS_CENTERED;

            SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_CONTEXT_MAJOR_VERSION, 3);
            SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_CONTEXT_MINOR_VERSION, 0);
            SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_CONTEXT_PROFILE_MASK, (int) SDL.SDL_GLprofile.SDL_GL_CONTEXT_PROFILE_CORE);

            int kStencilBits = 8;  // Skia needs 8 stencil bits
            SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_RED_SIZE, 8);
            SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_GREEN_SIZE, 8);
            SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_BLUE_SIZE, 8);
            SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_DOUBLEBUFFER, 1);
            SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_DEPTH_SIZE, 0);
            SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_STENCIL_SIZE, kStencilBits);

            SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_ACCELERATED_VISUAL, 1);

            _window = SDL.SDL_CreateWindow(title, x, y, w, h, WINDOW_FLAGS);

            // try and setup a GL context
            _glContext = SDL.SDL_GL_CreateContext(_window);
            if (_glContext == IntPtr.Zero) {
                throw new Exception("Unable to create OpenGL context.");
            }

            SDL.SDL_GL_MakeCurrent(_window, _glContext);

            var windowFormat = SDL.SDL_GetWindowPixelFormat(_window);
            int contextType;
            SDL.SDL_GL_GetAttribute(SDL.SDL_GLattr.SDL_GL_CONTEXT_PROFILE_MASK, out contextType);
            
            /*glViewport(0, 0, dw, dh);
            glClearColor(1, 1, 1, 1);
            glClearStencil(0);
            glClear(GL_COLOR_BUFFER_BIT | GL_STENCIL_BUFFER_BIT);*/
           
            var grInterface = GRGlInterface.CreateNativeGlInterface();
            var grContext = GRContext.Create(GRBackend.OpenGL, grInterface);

            _surface = SKSurface.Create(grContext, CreateRenderTarget());
        }

    
		private GRBackendRenderTargetDesc CreateRenderTarget()
        {
            int framebuffer, stencil, samples;
            Gles.glGetIntegerv(Gles.GL_FRAMEBUFFER_BINDING, out framebuffer);
            Gles.glGetIntegerv(Gles.GL_STENCIL_BITS, out stencil);
            Gles.glGetIntegerv(Gles.GL_SAMPLES, out samples);

            int bufferWidth, bufferHeight;
            SDL.SDL_GL_GetDrawableSize(_window, out bufferWidth, out bufferHeight);
            Width = bufferWidth;
            Height = bufferHeight;

            return new GRBackendRenderTargetDesc
            {
                Width = bufferWidth,
                Height = bufferHeight,
                Config = GRPixelConfig.Rgba8888,
                Origin = GRSurfaceOrigin.BottomLeft,
                SampleCount = samples,
                StencilBits = stencil,
                RenderTargetHandle = (IntPtr)framebuffer,
            };
        }

#region IDisposable Support
private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                }

                SDL.SDL_DestroyWindow(_window);

                disposedValue = true;
            }
        }

        ~Window()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

        public void PollEvents()
        {
            if (SDL.SDL_PollEvent(out SDL.SDL_Event evt) == 1)
            {
                ProcessEvent(ref evt);
            }
        }
        
        public void ProcessEvent(ref SDL.SDL_Event evt)
        {
            switch (evt.type)
            {
                case SDL.SDL_EventType.SDL_QUIT:
                    Closed = true;
                    break;
                case SDL.SDL_EventType.SDL_KEYDOWN:
                    if (evt.key.keysym.scancode == SDL.SDL_Scancode.SDL_SCANCODE_ESCAPE)
                    {
                        Closed = true;
                    }
                    break;
                case SDL.SDL_EventType.SDL_MOUSEWHEEL:
                    // window->mouseWheel(event.wheel.y);
                    break;
                case SDL.SDL_EventType.SDL_MOUSEBUTTONDOWN:
                    // window->mouseDown(event.button.button, event.button.x, event.button.y);
                    break;
                case SDL.SDL_EventType.SDL_MOUSEBUTTONUP:
                    // window->mouseUp(event.button.x, event.button.y);
                    break;
                case SDL.SDL_EventType.SDL_MOUSEMOTION:
                    // window->mouseMoved(event.motion.x, event.motion.y);
                    break;
                case SDL.SDL_EventType.SDL_WINDOWEVENT:
                    switch (evt.window.windowEvent)
                    {
                        case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_RESIZED:
                            // window->resize();
                            break;
                        default:
                            break;
                    }
                    break;
                default:
                    break;
            }
        }

        public void Swap()
        {
            SDL.SDL_GL_SwapWindow(_window);
        }

    }

}
