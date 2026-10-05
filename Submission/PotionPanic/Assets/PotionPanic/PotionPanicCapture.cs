using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace PotionPanic
{
    public sealed partial class PotionPanicApp
    {
        // This optional test runs only when the executable is started with -potion-capture <folder>.
        // It uses the actual Canvas buttons and checks pause, options, loss, crafting and screen sizes.
        void CheckCaptureArguments()
        {
            string[] args=Environment.GetCommandLineArgs();
            for(int i=0;i<args.Length-1;i++)if(args[i]=="-potion-capture")StartCoroutine(CaptureViews(args[i+1]));
        }
        void Check(bool condition,string description)
        { if(!condition){Debug.LogError("CHECK FAILED: "+description);Application.Quit(1);throw new InvalidOperationException(description);} }
        void Click(string caption)
        {
            foreach(Button b in page.GetComponentsInChildren<Button>())
                if(b.name==caption){Check(b.interactable,"Button enabled: "+caption);b.onClick.Invoke();return;}
            Check(false,"Missing button: "+caption);
        }
        IEnumerator CaptureView(string folder,string name)
        {
            yield return new WaitForSecondsRealtime(.2f);
            Canvas.ForceUpdateCanvases();
            yield return new WaitForEndOfFrame();
            Texture2D screenshot=ScreenCapture.CaptureScreenshotAsTexture();
            Check(screenshot!=null,"Screenshot exists");
            var pixels=screenshot.GetPixels32();bool visible=false;
            for(int i=0;i<pixels.Length;i+=211)if(pixels[i].r>100&&pixels[i].g>70){visible=true;break;}
            Check(visible,"Screen isn't blank: "+name);
            File.WriteAllBytes(Path.Combine(folder,name+".png"),screenshot.EncodeToPNG());
            Destroy(screenshot);
        }
        IEnumerator CaptureViews(string folder)
        {
            Directory.CreateDirectory(folder);
            string marker=Path.Combine(folder,"complete.txt");if(File.Exists(marker))File.Delete(marker);
            Application.runInBackground=true;
            float savedVolume=AudioListener.volume;
            int savedResolution=resolutionIndex;bool savedFullScreen=fullScreen;
            // Stop timers during pictures. The pause check below briefly restores real game time.
            Time.timeScale=0;
            yield return CaptureView(folder,"01-start");
            Click("Credits");yield return CaptureView(folder,"02-credits");Click("Back to start");
            Click("Play");difficulty=1;arrivalChoice=2;Render();
            yield return CaptureView(folder,"03-mode-select");Click("Start shift");
            Check(game.DayLength==180&&game.ArrivalInterval==8,"Selected mode settings reach the game");
            Time.timeScale=1;yield return new WaitForSecondsRealtime(.3f);Click("Pause");
            float frozen=game.Elapsed;yield return new WaitForSecondsRealtime(.4f);
            Check(game.Elapsed==frozen,"Pause freezes the day");yield return CaptureView(folder,"04-pause");
            Click("Options");var slider=page.GetComponentInChildren<Slider>();slider.value=.25f;
            Check(Mathf.Abs(AudioListener.volume-.25f)<.001f,"Volume slider changes audio volume");
            yield return new WaitForSecondsRealtime(.3f);Check(game.Elapsed==frozen,"Options keeps the game paused");
            yield return CaptureView(folder,"05-options");
            Click("1024 x 768");yield return new WaitForSecondsRealtime(.8f);
            Check(Screen.width==1024&&Screen.height==768,"4:3 resolution applies");
            yield return CaptureView(folder,"06-options-4x3");
            Click("1280 x 720");yield return new WaitForSecondsRealtime(.8f);
            Check(Screen.width==1280&&Screen.height==720,"16:9 resolution applies");
            yield return CaptureView(folder,"07-options-16x9");
            Click("1440 x 900");yield return new WaitForSecondsRealtime(.8f);
            Click("Save & back");Check(menu==Menu.Pause,"Options returns to pause");Click("Resume shift");
            yield return new WaitForSecondsRealtime(.3f);Check(game.Elapsed>frozen,"Resume restarts the day clock");
            Time.timeScale=0;game.Tick(game.DayLength);Go(Menu.Results);
            Check(game.Failed&&!game.Won,"Deadline produces a loss");yield return CaptureView(folder,"08-loss");
            Click("Try another shift");Click("Guided practice");
            yield return CaptureView(folder,"09-orders");Click("Take order");
            yield return CaptureView(folder,"10-ingredients");
            // Send pointer events to the real hold component, then let Update do the pouring.
            Time.timeScale=1;pourButton.OnPointerDown(new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left});
            yield return new WaitForSecondsRealtime(.25f);pourButton.OnPointerUp(new PointerEventData(EventSystem.current));
            Check(selected.Amounts[0]>0,"Holding the UI button pours ingredients");Time.timeScale=0;
            game.Clear(selected);
            for(int i=0;i<3;i++)game.Pour(selected,i,PotionGame.Targets[selected.Recipe,i]/22f);
            Click("Send to brewing");Click("Start brewing");game.Tick(9);
            yield return CaptureView(folder,"11-brewing");Click("Stop brewing");
            game.Stir(selected,2.8f);yield return CaptureView(folder,"12-stirring");Click("Finish");
            Click("Round flask");Click("Stardust");Click("#1");
            yield return CaptureView(folder,"13-finishing");Click("Serve potion");
            Check(game.Won&&game.Served==1&&game.Earnings==25,"Practice completes through the Canvas controls");
            yield return CaptureView(folder,"14-practice-win");
            difficulty=0;arrivalChoice=0;Begin(false);
            // Complete a five-customer day too, to inspect all rows of the final ledger.
            for(int i=0;i<5;i++)
            {
                if(i>0)game.Tick(game.ArrivalInterval);
                Order o=game.Orders[i];game.Take(o);
                for(int j=0;j<3;j++)game.Pour(o,j,PotionGame.Targets[o.Recipe,j]/22f);
                game.Prepare(o);game.StartBrew(o);game.Tick(9);game.StopBrew(o);game.Stir(o,2.8f);game.FinishStir(o);
                o.ChosenBottle=o.Bottle;o.ChosenGarnish=o.Garnish;game.Serve(o,o.Number);
            }
            Check(game.Won&&!game.Failed&&game.Served==5,"Five-customer shift can be won");Go(Menu.Results);
            yield return CaptureView(folder,"15-day-win");
            AudioListener.volume=savedVolume;PlayerPrefs.SetFloat("Potion.Volume",savedVolume);
            resolutionIndex=savedResolution;fullScreen=savedFullScreen;ApplyDisplay();
            File.WriteAllText(marker,"PASS: 15 Canvas screens, mode settings, pause/resume, paused options, audio slider, resolution changes, hold input, loss, practice win and five-customer win. Unity "+Application.unityVersion);
            Application.Quit();
        }
    }
}
