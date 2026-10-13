using System;
using System.Collections;
using System.IO;
using System.Linq;
using ColdWarStruggle.Presentation;
using UnityEngine;

namespace ColdWarStruggle.Bootstrap
{
    // Explicit diagnostic export of this game's rendered frame, never a desktop capture.
    public sealed class DemoCapture : MonoBehaviour
    {
        bool busy;
        DemoView view;

        void Start()
        {
            view = GetComponent<DemoView>();
            if (Environment.GetCommandLineArgs().Contains("--capture-demo"))
            {
                UnityEngine.Application.runInBackground = true;
                StartCoroutine(Capture(true));
            }
        }

        void Update()
        {
            if (!busy && Input.GetKeyDown(KeyCode.F12)) StartCoroutine(Capture(false));
        }

        IEnumerator Capture(bool exitAfterCapture)
        {
            busy = true;
            // Allow the data/view initialization and font atlas to render before exporting.
            yield return null;
            yield return null;
            yield return new WaitForEndOfFrame();
            Texture2D texture = null;
            string output = null;
            bool created = false;
            int exitCode = 1;
            try
            {
                if (exitAfterCapture && (view == null || !view.IsReady))
                    throw new InvalidOperationException("Demo initialization failed; refusing a successful capture of the error page.");
                texture = ScreenCapture.CaptureScreenshotAsTexture();
                if (texture == null || texture.width < 1 || texture.height < 1)
                    throw new InvalidOperationException("No rendered game frame is available.");
                var png = texture.EncodeToPNG();
                if (png == null || png.Length == 0) throw new InvalidOperationException("PNG encoding failed.");
                var folder = Path.Combine(UnityEngine.Application.persistentDataPath, "DemoCaptures");
                Directory.CreateDirectory(folder);
                var path = Path.Combine(folder, DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N") + ".png");
                output = path;
                using (var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    created = true;
                    stream.Write(png, 0, png.Length);
                }
                Debug.Log("DEMO_CAPTURE_SAVED " + path);
                view?.SetCaptureStatus("画面已保存到本机 DemoCaptures 文件夹");
                exitCode = 0;
            }
            catch (Exception exception)
            {
                if (created)
                {
                    try { File.Delete(output); }
                    catch (Exception cleanup) { Debug.LogWarning("Could not remove incomplete capture: " + cleanup.Message); }
                }
                Debug.LogException(exception);
                view?.SetCaptureStatus("画面保存失败，请查看日志");
            }
            finally
            {
                if (texture != null) Destroy(texture);
                busy = false;
            }
            if (exitAfterCapture && !UnityEngine.Application.isEditor) UnityEngine.Application.Quit(exitCode);
        }
    }
}
