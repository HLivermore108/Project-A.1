using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
#endif
using static PotionPanic.PotionUI;

namespace PotionPanic
{
    // The rules live in PotionGame. This script connects them to Canvas menus and mouse controls.
    public sealed partial class PotionPanicApp : MonoBehaviour
    {
        public enum Menu { Start, Modes, Orders, Ingredients, Brewing, Finishing, Pause, Options, Credits, Results }
        [SerializeField] string creatorName = "";
        Menu menu = Menu.Start, returnFromPause, returnFromOptions;
        PotionGame game;
        Order selected;
        int ingredient, matchingTicket, difficulty, arrivalChoice, resolutionIndex = 1;
        bool fullScreen;
        Canvas canvas;
        RectTransform board, page;
        PotionHoldButton pourButton, stirButton;
        AudioSource speaker;
        AudioClip chime;
        Text messageText;
        float messageUntil;
        string lastMessage = "";
        // Only labels/meters that change each frame go here. Buttons and art aren't rebuilt each frame.
        readonly List<Action> live = new List<Action>();
        readonly Dictionary<Menu, RectTransform> menus = new Dictionary<Menu, RectTransform>();
        readonly int[] daySeconds = { 240, 180, 135 };
        readonly string[] difficultyNames = { "Apprentice", "Shopkeeper", "Potion master" };
        readonly int[] arrivalSeconds = { 22, 14, 8 };
        readonly string[] arrivalNames = { "Steady", "Busy", "Rush" };
        readonly Vector2Int[] sizes = { new Vector2Int(1280,720), new Vector2Int(1440,900), new Vector2Int(1920,1080), new Vector2Int(1024,768) };
        bool Playing => menu >= Menu.Orders && menu <= Menu.Finishing;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            // FindObjectOfType also works in the required Unity 2022.1 editor.
            if (FindObjectOfType<PotionPanicApp>() == null) new GameObject("Potion Panic Game").AddComponent<PotionPanicApp>();
        }
        void Start()
        {
            Application.targetFrameRate = 60;
            SetupCanvas(); SetupAudio();
            AudioListener.volume = Mathf.Clamp01(PlayerPrefs.GetFloat("Potion.Volume", .7f));
            resolutionIndex = Mathf.Clamp(PlayerPrefs.GetInt("Potion.Resolution",1),0,sizes.Length-1);
            fullScreen = PlayerPrefs.GetInt("Potion.Fullscreen",0) == 1;
            if (PlayerPrefs.HasKey("Potion.Resolution")) ApplyDisplay();
            Render();
            CheckCaptureArguments();
        }
        void SetupCanvas()
        {
            // Each screen is made from real uGUI components under this Canvas.
            // Expand makes the whole design fit even on a 4:3 display, with extra space around it.
            canvas = new GameObject("Potion Panic Canvas",typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster)).GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1440,900);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            var background = Panel(canvas.transform,"Background",0,0,0,0,C("211C2B")).rectTransform;
            background.anchorMin = Vector2.zero; background.anchorMax = Vector2.one; background.offsetMin = background.offsetMax = Vector2.zero;
            board = Rect("Design board",canvas.transform,0,0,1440,900);
            board.anchorMin = board.anchorMax = board.pivot = new Vector2(.5f,.5f); board.anchoredPosition = Vector2.zero;
            // Keep ten named menu panels in the hierarchy, with only the current one active.
            foreach (Menu screen in Enum.GetValues(typeof(Menu)))
            {
                var panel = Rect(screen + " Menu", board, 0, 0, 1440, 900);
                panel.gameObject.SetActive(false);
                menus.Add(screen, panel);
            }
            if (EventSystem.current == null)
            {
                var events = new GameObject("UI EventSystem",typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
                events.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
#else
                events.AddComponent<StandaloneInputModule>();
#endif
            }
        }
        void SetupAudio()
        {
            speaker = gameObject.AddComponent<AudioSource>();
            chime = AudioClip.Create("Potion chime",11025,1,44100,false);
            var samples = new float[11025];
            for(int i=0;i<samples.Length;i++) samples[i]=Mathf.Sin(i*2*Mathf.PI*660/44100)*.2f*(1-i/(float)samples.Length);
            chime.SetData(samples,0);
            if (FindObjectOfType<AudioListener>() == null) gameObject.AddComponent<AudioListener>();
        }
        void OnDestroy()
        {
            if (canvas != null) Destroy(canvas.gameObject);
            if (chime != null) Destroy(chime);
        }
        void OnApplicationFocus(bool focused) { if (!focused && Playing) Pause(); }
        void Update()
        {
#if ENABLE_INPUT_SYSTEM
            bool escape = Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
#else
            bool escape = Input.GetKeyDown(KeyCode.Escape);
#endif
            if (escape)
            {
                if (Playing) Pause();
                else if (menu == Menu.Pause) Go(returnFromPause);
                else if (menu == Menu.Options) CloseOptions();
                else if (menu == Menu.Credits || menu == Menu.Modes) Go(Menu.Start);
            }
            // Pause and Options never reach Tick, so all customer, day and cauldron timers stop together.
            if (Playing && game != null && !game.Complete)
            {
                int count = game.Orders.Count;
                Phase before = selected == null ? Phase.Waiting : selected.Phase;
                game.Tick(Time.deltaTime);
                if (pourButton != null && pourButton.Held) game.Pour(selected,ingredient,Time.deltaTime);
                if (stirButton != null && stirButton.Held) game.Stir(selected,Time.deltaTime);
                if (game.Complete) Go(Menu.Results);
                else if (count != game.Orders.Count || (selected != null && before != selected.Phase)) Render();
            }
            foreach(Action refresh in live) refresh();
            if(messageText != null) messageText.text = Time.unscaledTime < messageUntil ? lastMessage : "";
        }
        void Begin(bool practice)
        {
            game = new PotionGame(practice,daySeconds[difficulty],arrivalSeconds[arrivalChoice]);
            selected = null; ingredient = matchingTicket = 0; lastMessage = "";
            Go(Menu.Orders);
        }
        void Go(Menu next) { menu = next; Render(); }
        void Pause() { returnFromPause = menu; Go(Menu.Pause); }
        void Feedback(string text) { lastMessage=text; messageUntil=Time.unscaledTime+5; speaker.PlayOneShot(chime); }
        void OpenOptions() { returnFromOptions=menu; Go(Menu.Options); }
        void CloseOptions() { PlayerPrefs.Save(); Go(returnFromOptions); }
        void ApplyDisplay()
        {
            Vector2Int size=sizes[resolutionIndex];
            Screen.SetResolution(size.x,size.y,fullScreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed);
            PlayerPrefs.SetInt("Potion.Resolution",resolutionIndex); PlayerPrefs.SetInt("Potion.Fullscreen",fullScreen?1:0); PlayerPrefs.Save();
        }
        Text Dynamic(Func<string> value,float x,float y,float w,float h,int size=18,Color? color=null)
        {
            Text text=Label(page,value(),x,y,w,h,size,color);
            live.Add(()=>text.text=value()); return text;
        }
        void Gauge(Func<float> value,float x,float y,float width,Color color,float target=-1,float zone=0)
        {
            var fill=Meter(page,x,y,width,color,target,zone);
            live.Add(()=>fill.sizeDelta=new Vector2(width*Mathf.Clamp01(value()/100),12));
        }
        Button ActionButton(string text,float x,float y,float w,Action action,bool accent=false,Func<bool> allowed=null)
        {
            var button=Button(page,text,x,y,w,52,()=>{ action(); },accent);
            if(allowed!=null) live.Add(()=>button.interactable=allowed());
            return button;
        }
        // Rendering a menu happens when a button changes the screen or an order changes stage.
        void Render()
        {
            if(board==null) return;
            if(page!=null) page.gameObject.SetActive(false);
            live.Clear(); pourButton=stirButton=null; messageText=null;
            page=menus[menu];
            foreach(Transform child in page) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
            page.gameObject.SetActive(true);
            switch(menu)
            {
                case Menu.Start: StartMenu(); break;
                case Menu.Modes: ModeMenu(); break;
                case Menu.Credits: CreditsMenu(); break;
                case Menu.Options: OptionsMenu(); break;
                case Menu.Pause: PauseMenu(); break;
                case Menu.Results: ResultsMenu(); break;
                default:
                    ShopHeader();
                    switch(menu) { case Menu.Orders: OrdersMenu(); break; case Menu.Ingredients: IngredientsMenu(); break; case Menu.Brewing: BrewingMenu(); break; case Menu.Finishing: FinishingMenu(); break; }
                    Tickets(); Status(); break;
            }
            foreach(Action refresh in live) refresh();
        }
        void Heading(string eyebrow,string title,string description)
        {
            Label(page,eyebrow,60,193,970,28,14,Gold,true);
            Label(page,title,58,234,980,52,34,Ink,true);
            Label(page,description,60,295,970,58,18,Muted);
        }
        void MenuHeading(string eyebrow,string title,string description)
        {
            Label(page,eyebrow,90,64,1260,30,16,Gold,true);
            Label(page,title,86,115,1270,75,48,Ink,true);
            Label(page,description,90,207,1250,62,21,Muted);
        }
        void StartMenu()
        {
            Label(page,"THE LITTLE MOON APOTHECARY",90,108,740,32,18,Gold,true);
            Label(page,"Potion\nPanic",82,174,720,260,102,Ink,true);
            Label(page,"A pinch of magic. A dash of hurry.",92,464,750,46,28,Gold);
            Label(page,"Serve five fantasy customers before closing time.\nMeasure, brew and bottle their perfect potion.",94,526,750,80,22,Muted);
            for(int row=0;row<2;row++)
            {
                for(int i=0;i<5;i++) Bottle(page,849+i*87,177+row*154,.7f,(i+row)%2,Liquids[(i+row)%3]);
                Panel(page,"Wooden shelf",832,282+row*154,476,16,C("79584D"));
            }
            Cauldron(page,927,474,Liquids[1]);
            ActionButton("Play",94,649,280,()=>Go(Menu.Modes),true);
            ActionButton("Options",392,649,190,OpenOptions);
            ActionButton("Credits",600,649,190,()=>Go(Menu.Credits));
            ActionButton("Quit",94,725,190,()=>Application.Quit());
            Label(page,"MOUSE TO PLAY  /  HOLD TO POUR & STIR  /  ESC TO PAUSE",94,835,1240,28,15,Muted);
        }
        void ModeMenu()
        {
            MenuHeading("BEFORE THE DOORS OPEN","Choose your shift.","Serve all five customers before the clock runs out. Every potion earns coins; accuracy earns tips.");
            Label(page,"SHIFT LENGTH",94,312,1100,32,17,Gold,true);
            for(int i=0;i<3;i++) { int choice=i; ActionButton(difficultyNames[i]+"  /  "+daySeconds[i]+" seconds",94+i*414,362,396,()=>{difficulty=choice;Render();},difficulty==i); }
            Label(page,"CUSTOMER ARRIVALS",94,458,1100,32,17,Gold,true);
            for(int i=0;i<3;i++) { int choice=i; ActionButton(arrivalNames[i]+"  /  every "+arrivalSeconds[i]+" seconds",94+i*414,508,396,()=>{arrivalChoice=choice;Render();},arrivalChoice==i); }
            Label(page,"New to the shop? Practice is one guided order with no closing-time deadline.",94,612,1250,52,20,Muted);
            ActionButton("Start shift",94,715,310,()=>Begin(false),true);
            ActionButton("Guided practice",426,715,310,()=>Begin(true));
            ActionButton("Back",758,715,200,()=>Go(Menu.Start));
        }
        void CreditsMenu()
        {
            MenuHeading("BEHIND THE COUNTER","Credits","Potion Panic - a single-player potion shop game.");
            Label(page,string.IsNullOrWhiteSpace(creatorName)?"A game design class project": "Created by "+creatorName,94,327,1250,48,30,Gold,true);
            Label(page,"CONCEPT\nBased on the Potion Panic game proposal.\n\nART & AUDIO\nPotion bottles, shop decorations and sound effects are generated in the project.\n\nTOOLS\nBuilt with Unity. Menus use Unity UI (uGUI); the font is supplied by Unity.",94,405,1220,300,22,Ink);
            ActionButton("Back to start",94,779,320,()=>Go(Menu.Start),true);
        }
        void OptionsMenu()
        {
            MenuHeading("MAKE YOURSELF AT HOME","Options",game!=null&&!game.Complete&&returnFromOptions!=Menu.Start?"Your shift is paused while this menu is open.":"Audio and display settings are kept the next time you open the game.");
            Label(page,"SOUND VOLUME",94,315,550,30,17,Gold,true);
            var sliderRoot=Rect("Volume Slider",page,94,378,680,40);
            var slider=sliderRoot.gameObject.AddComponent<Slider>(); slider.minValue=0; slider.maxValue=1;
            Panel(sliderRoot,"Track",0,15,680,10,C("50455D"));
            var handleArea=Rect("Handle area",sliderRoot,10,0,660,40);
            var handle=Panel(handleArea,"Handle",0,0,24,40,Gold); handle.raycastTarget=true;
            slider.handleRect=handle.rectTransform; slider.targetGraphic=handle;
            slider.SetValueWithoutNotify(AudioListener.volume);
            slider.onValueChanged.AddListener(v=>{ AudioListener.volume=v; PlayerPrefs.SetFloat("Potion.Volume",v); });
            Dynamic(()=>Mathf.RoundToInt(AudioListener.volume*100)+"%",812,381,150,40,24,Ink);
            ActionButton("Test sound",999,371,270,()=>speaker.PlayOneShot(chime),true);
            Label(page,"WINDOW RESOLUTION",94,480,1150,28,17,Gold,true);
            for(int i=0;i<sizes.Length;i++)
            { int choice=i; ActionButton(sizes[i].x+" x "+sizes[i].y,94+i*310,527,291,()=>{resolutionIndex=choice;ApplyDisplay();Render();},resolutionIndex==i); }
            ActionButton(fullScreen?"Fullscreen: on":"Fullscreen: off",94,626,380,()=>{fullScreen=!fullScreen;ApplyDisplay();Render();},fullScreen);
            Label(page,"The UI scales to fit each resolution and aspect ratio.\nFullscreen uses your desktop display size.",505,630,790,72,19,Muted);
            ActionButton("Save & back",94,779,320,CloseOptions,true);
        }
        void PauseMenu()
        {
            MenuHeading("SHOP ON HOLD","A moment to breathe.","The closing clock, customer patience and both cauldrons are paused.");
            ActionButton("Resume shift",94,335,600,()=>Go(returnFromPause),true);
            ActionButton("Options",94,411,600,OpenOptions);
            ActionButton("Restart shift",94,487,600,()=>Begin(game.Practice));
            ActionButton("Return to start",94,563,600,()=>{game=null;selected=null;Go(Menu.Start);});
            Cauldron(page,927,422,Liquids[1]);
        }
        void ShopHeader()
        {
            Label(page,"POTION PANIC",32,22,490,50,34,Ink,true);
            Dynamic(()=>game.Practice?"GUIDED PRACTICE / NO DEADLINE":$"CLOSING IN {Mathf.CeilToInt(game.Remaining)/60:00}:{Mathf.CeilToInt(game.Remaining)%60:00}  /  SERVE FIVE TO WIN",34,77,1000,28,16,Gold);
            Dynamic(()=>$"{game.Served}/{(game.Practice?1:5)} served  /  {game.Earnings} coins",817,38,380,40,22,Gold);
            ActionButton("Pause",1220,25,190,Pause);
            string[] names={"Orders","Ingredients","Brewing","Finishing"};
            for(int i=0;i<4;i++) { Menu target=(Menu)((int)Menu.Orders+i); ActionButton("0"+(i+1)+"  "+names[i],32+i*265,116,251,()=>Go(target),menu==target); }
            Panel(page,"Station panel",32,183,1046,568,C("2D2639"));
        }
        void Select(Order order)
        {
            selected=order; matchingTicket=0;
            Go(order.Phase==Phase.Measuring?Menu.Ingredients:order.Phase==Phase.Finishing?Menu.Finishing:Menu.Brewing);
        }
        bool NeedOrder()
        {
            if(selected!=null&&selected.Phase!=Phase.Served) return false;
            Label(page,"Every potion starts with a customer.",122,411,900,60,30,Ink,true);
            ActionButton("Go to the order counter",270,518,555,()=>Go(Menu.Orders),true); return true;
        }
        void WrongStation()
        {
            Label(page,"This potion is "+PhaseText(selected).ToLower()+".",110,411,900,65,29,Ink,true);
            ActionButton("Continue this potion",270,518,555,()=>Select(selected),true);
        }
        void OrdersMenu()
        {
            Heading("WELCOME TO YOUR COUNTER","A little magic, made to order.","Take a customer's order to start a ticket. You can have up to three active tickets at once.");
            int n=0;
            foreach(Order order in game.Orders)
            {
                if(order.Phase==Phase.Served) continue;
                float x=60+(n%3)*330,y=366+(n/3)*171; n++;
                Panel(page,"Customer "+order.Number,x,y,310,154,C("3B3147"));
                Circle(page,x+13,y+17,72,72,order.Number%3==0?Green:C("DEC0A2"));
                Label(page,order.Number%3==0?"G":order.Number%3==1?"W":"K",x+34,y+31,45,42,31,C("302536"),true);
                Label(page,order.Customer,x+98,y+19,205,48,20,Ink,true);
                Label(page,order.Phase==Phase.Waiting?"Ready to order":"Ticket #"+order.Number,x+98,y+72,205,28,16,Muted);
                ActionButton(order.Phase==Phase.Waiting?"Take order":"View ticket",x+13,y+99,284,()=>{if(order.Phase==Phase.Waiting)game.Take(order);Select(order);},order.Phase==Phase.Waiting,()=>order.Phase!=Phase.Waiting||game.ActiveCount<3);
            }
            if(n==0) Label(page,"A quiet moment. Your next guest is on the way.",90,431,950,70,27,Muted);
        }
        void IngredientsMenu()
        {
            Heading("THE MEASURING TABLE","A pinch. A pour. A perfect balance.","Select an ingredient, hold Pour and release at the ticket's amount. Empty the mixture to try again.");
            if(NeedOrder())return;
            if(selected.Phase!=Phase.Measuring){WrongStation();return;}
            for(int i=0;i<3;i++)
            {
                int choice=i; float x=60+i*220;
                Panel(page,"Ingredient card",x,367,200,223,ingredient==i?C("53415D"):C("3A3045"));
                Bottle(page,x+66,385,.65f,i==1?1:0,Liquids[i]);
                Label(page,i==0?"BERRIES / RED":i==1?"FLOWERS / BLUE":"LEAVES / GREEN",x+13,486,185,30,14,Liquids[i],true);
                ActionButton(PotionGame.Ingredients[i],x+10,528,180,()=>{ingredient=choice;Render();},ingredient==i);
                Dynamic(()=>$"{PotionGame.Ingredients[choice]}: {selected.Amounts[choice]:0} / {PotionGame.Targets[selected.Recipe,choice]} ml",752,396+i*71,295,31,16);
                Gauge(()=>selected.Amounts[choice],752,437+i*71,280,Liquids[i],PotionGame.Targets[selected.Recipe,i]);
            }
            pourButton=ActionButton("Hold to pour "+PotionGame.Ingredients[ingredient].ToLower(),60,618,422,null,true).gameObject.AddComponent<PotionHoldButton>();
            ActionButton("Empty mixture",502,618,198,()=>{game.Clear(selected);Feedback("Mixture emptied. Try another pour.");});
            ActionButton("Send to brewing",752,649,280,()=>{game.Prepare(selected);Go(Menu.Brewing);},true);
        }
        void BrewingMenu()
        {
            Heading("THE CAULDRON ROOM","Good things take just enough time.","Stop the brew at 8-10 seconds. Then hold Stir to 70, release, and finish. Two cauldrons are available.");
            if(NeedOrder())return;
            if(selected.Phase!=Phase.ReadyToBrew&&selected.Phase!=Phase.Brewing&&selected.Phase!=Phase.Stirring){WrongStation();return;}
            Cauldron(page,145,438,Liquids[selected.Recipe]);
            Label(page,"TICKET #"+selected.Number+" / "+PotionGame.Recipes[selected.Recipe],105,689,440,35,18,Gold,true);
            Dynamic(()=>PhaseText(selected),566,383,466,48,29,Gold);
            if(selected.Phase==Phase.ReadyToBrew)
            {
                Label(page,"The brew keeps heating while you work at other stations. Stirring also occupies a cauldron.",566,460,461,98,22,Muted);
                ActionButton("Start brewing",566,591,461,()=>{game.StartBrew(selected);Render();},true,()=>game.BrewingCount<2);
                Dynamic(()=>$"{game.BrewingCount}/2 cauldrons occupied",566,659,461,33,17,Muted);
            }
            else if(selected.Phase==Phase.Brewing)
            {
                Dynamic(()=>selected.BrewTime>=8&&selected.BrewTime<=10?"NOW! The magic is just right.":selected.BrewTime>10?"Too hot! Stop the brew.":"The magic is warming up...",566,454,461,57,23,Green);
                Gauge(()=>selected.BrewTime/15*100,566,530,461,Liquids[selected.Recipe],60,6.6667f);
                Dynamic(()=>$"{selected.BrewTime:0.0} seconds / perfect: 8-10",566,566,461,34,18,Muted);
                ActionButton("Stop brewing",566,626,461,()=>{game.StopBrew(selected);Render();},true);
            }
            else
            {
                Label(page,selected.BrewTime>=15?"This brew overheated. You can still finish it for a lower score.":"Hold Stir and release at the marker. Going past it loses points.",566,454,461,69,21,Muted);
                Gauge(()=>selected.StirAmount,566,548,461,Liquids[selected.Recipe],70,3);
                Dynamic(()=>$"Stirring: {selected.StirAmount:0} / 70",566,589,461,31,18);
                stirButton=ActionButton("Hold to stir",566,638,276,null,true).gameObject.AddComponent<PotionHoldButton>();
                ActionButton("Finish",859,638,168,()=>{game.FinishStir(selected);Go(Menu.Finishing);},true);
            }
        }
        void FinishingMenu()
        {
            Heading("THE BOTTLING BENCH","The final touch is everything.","Choose the requested bottle and garnish, then match the numbered ticket and serve the potion.");
            if(NeedOrder())return;
            if(selected.Phase!=Phase.Finishing){WrongStation();return;}
            Bottle(page,149,435,1.6f,Mathf.Max(0,selected.ChosenBottle),Liquids[selected.Recipe]);
            if(selected.ChosenGarnish>=0)Label(page,selected.ChosenGarnish==0?"*":"leaf",273,443,100,50,28,selected.ChosenGarnish==0?Gold:Green,true);
            Label(page,"BOTTLE",412,375,600,30,16,Gold,true);
            for(int i=0;i<2;i++){int choice=i;ActionButton(PotionGame.Bottles[i],412+i*307,416,289,()=>{selected.ChosenBottle=choice;Render();},selected.ChosenBottle==i);}
            Label(page,"GARNISH",412,491,600,30,16,Gold,true);
            for(int i=0;i<2;i++){int choice=i;ActionButton(PotionGame.Garnishes[i],412+i*307,530,289,()=>{selected.ChosenGarnish=choice;Render();},selected.ChosenGarnish==i);}
            Label(page,"MATCH A TICKET",412,610,600,30,16,Gold,true);
            int n=0;
            foreach(Order order in game.Orders) if(order.Phase!=Phase.Waiting&&order.Phase!=Phase.Served)
            {ActionButton("#"+order.Number,412+n*104,653,90,()=>{matchingTicket=order.Number;Render();},matchingTicket==order.Number);n++;}
            ActionButton("Serve potion",753,653,255,()=>
            {
                if(!game.Serve(selected,matchingTicket)){Feedback("Choose the ticket that matches this potion's number.");return;}
                Feedback($"{selected.Customer}: {selected.Overall:0}% / +{selected.Payment+selected.Tip} coins");
                selected=null;Go(game.Complete?Menu.Results:Menu.Orders);
            },true,()=>selected.ChosenBottle>=0&&selected.ChosenGarnish>=0&&matchingTicket>0);
        }
        string PhaseText(Order order)
        {
            switch(order.Phase)
            {
                case Phase.Measuring:return "Measuring";case Phase.ReadyToBrew:return "Ready to brew";
                case Phase.Brewing:return order.BrewTime>10?"Getting too hot!":order.BrewTime>=8?"Stop now!":"Brewing";
                case Phase.Stirring:return "Ready to stir";case Phase.Finishing:return "Ready to bottle";case Phase.Served:return "Served";default:return "Waiting";
            }
        }
        void Tickets()
        {
            Label(page,"YOUR TICKETS",1102,131,300,32,20,Gold,true);
            int n=0;
            foreach(Order order in game.Orders)
            {
                if(order.Phase==Phase.Waiting||order.Phase==Phase.Served)continue;
                float y=183+n*191;n++;
                var card=Button(page,"",1100,y,310,179,()=>Select(order),false);
                card.name="Ticket #"+order.Number;card.GetComponent<Image>().color=selected==order?C("5A4557"):C("3A3045");
                Label(card.transform,"#"+order.Number+"  "+PotionGame.Recipes[order.Recipe],16,10,282,31,22,Ink,true);
                Label(card.transform,order.Customer,16,45,282,24,15,Muted);
                string recipe="";for(int i=0;i<3;i++)if(PotionGame.Targets[order.Recipe,i]>0)recipe+=$"{PotionGame.Targets[order.Recipe,i]} ml {PotionGame.Ingredients[i]}\n";
                Label(card.transform,recipe,16,74,282,49,16);
                Label(card.transform,PotionGame.Bottles[order.Bottle]+" + "+PotionGame.Garnishes[order.Garnish],16,126,282,25,15,Gold);
                var state=Label(card.transform,"",16,151,282,25,15,Green);
                live.Add(()=>state.text=PhaseText(order)+$" / {order.Wait:0}s");
            }
            if(n==0)Label(page,"A clean slate.\nTake an order to pin\nyour first ticket here.",1120,226,270,133,21,Muted);
        }
        void Status()
        {
            Panel(page,"Status panel",32,774,1046,104,C("352B40"));
            Label(page,game.Practice?"APPRENTICE'S NOTE":"CAULDRON WATCH",52,790,980,25,14,Gold,true);
            Dynamic(()=>game.Practice?PracticeHint():CauldronWatch(),52,825,1000,49,18);
            messageText=Label(page,lastMessage,1100,790,310,89,17,Gold);
        }
        string PracticeHint()
        {
            if(selected==null)return "Click Mabel to take your first order.";
            switch(selected.Phase)
            {case Phase.Measuring:return "Pour 60 ml berries and 40 ml flowers, then send the mixture to brewing.";
             case Phase.ReadyToBrew:return "Start the brew. Stop it when it reaches the 8-10 second green zone.";
             case Phase.Brewing:return "Click Stop at 8-10 seconds. Other potions keep brewing when you switch tickets.";
             case Phase.Stirring:return "Hold Stir until 70, release, then click Finish.";
             default:return "Choose a round flask and stardust. Match ticket #1, then serve.";}
        }
        string CauldronWatch()
        {
            string text="";foreach(Order o in game.Orders)if(o.Phase==Phase.Brewing||o.Phase==Phase.Stirring)text+=$"#{o.Number} {PhaseText(o)} ({o.BrewTime:0.0}s)   ";
            return text==""?"Both fires are quiet. Check the counter for waiting customers.":text;
        }
        void ResultsMenu()
        {
            bool lost=game.Failed;
            MenuHeading(lost?"SHIFT LOST / THE SHOP IS CLOSED":game.Practice?"APPRENTICESHIP COMPLETE":"SHIFT COMPLETE",lost?"Closing time caught you.":"The last cork is in.",lost?$"You served {game.Served} of 5 customers. Try a longer shift or different arrival timing.":game.Practice?"You've learned the full recipe. Ready to open the shop?":"Five happy deliveries. See where your next shift could be even better.");
            float average=0;foreach(Order o in game.Orders)if(o.Phase==Phase.Served)average+=o.Overall;
            if(game.Served>0)average/=game.Served;
            Label(page,$"{game.Earnings} coins  /  {game.TotalTips} tips included  /  {average:0}% average",94,293,1240,55,29,Gold,true);
            Panel(page,"Ledger header",94,375,1250,46,C("49394F"));
            string[] headings={"CUSTOMER","INGREDIENTS","BREWING","FINISHING","PATIENCE","OVERALL"};
            Label(page,headings[0],112,387,330,26,15,Gold,true);
            for(int i=1;i<6;i++)Label(page,headings[i],455+(i-1)*177,387,173,26,14,Gold,true);
            int n=0;
            foreach(Order o in game.Orders)
            {
                float y=430+n*50;n++;
                Panel(page,"Customer result",94,y,1250,45,n%2==0?C("2E2738"):C("393044"));
                Label(page,o.Customer,112,y+9,330,30,18);
                if(o.Phase!=Phase.Served){Label(page,"Not served before closing",455,y+9,840,30,18,Muted);continue;}
                float[] scores={o.IngredientsRating,o.BrewRating,o.FinishRating,o.WaitRating,o.Overall};
                for(int i=0;i<5;i++)Label(page,$"{scores[i]:0}%",455+i*177,y+9,173,30,18,scores[i]>=80?Green:Ink);
            }
            ActionButton(game.Practice?"Choose a shift":"Try another shift",94,774,370,()=>Go(Menu.Modes),true);
            ActionButton("Back to start",486,774,290,()=>{game=null;selected=null;Go(Menu.Start);});
        }
    }
}
