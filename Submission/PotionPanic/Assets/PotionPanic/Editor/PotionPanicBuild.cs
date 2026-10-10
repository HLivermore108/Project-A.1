using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PotionPanic.Editor
{
    public static class PotionPanicBuild
    {
        const string ScenePath="Assets/Scenes/PotionPanic.unity";
        [MenuItem("Potion Panic/Set up music")]
        public static void PrepareMusic()
        {
            const string resourceFolder = "Assets/PotionPanic/Resources";
            if (!AssetDatabase.IsValidFolder(resourceFolder)) AssetDatabase.CreateFolder("Assets/PotionPanic", "Resources");
            string path = resourceFolder + "/PotionMusic.asset";
            var library = AssetDatabase.LoadAssetAtPath<PotionMusicLibrary>(path);
            if (library == null) { library = ScriptableObject.CreateInstance<PotionMusicLibrary>(); AssetDatabase.CreateAsset(library, path); }
            library.mainMenu = MusicClip("Assets/LudoLoon Studio/Tabletop Jazz Cafe/Tabletop Jazz Cafe.wav");
            library.gameplay = MusicClip("Assets/Casual & Relaxing Game Music/Forest.wav");
            library.results = MusicClip("Assets/RedsenGameMusic_Afternoon/RedsenGameMusic_Afternoon_Cute_Casual_Loopable.wav");
            library.orderComplete = MusicClip("Assets/HintsStarsLite/Shimmery Reward 5.wav");
            library.newCustomer = MusicClip("Assets/HintsStarsLite/Notified 2.wav");
            library.brewReady = MusicClip("Assets/HintsStarsLite/Marimba 3 Notes Descend.wav");
            library.startBrewing = MusicClip("Assets/HintsStarsLite/Magic Score 9.wav");
            library.finishBrewing = MusicClip("Assets/HintsStarsLite/Retro Star 2.wav");
            EditorUtility.SetDirty(library); AssetDatabase.SaveAssets();
        }
        static AudioClip MusicClip(string path)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (clip == null) throw new Exception("Missing music: " + path);
            return clip;
        }
        // This command creates a clean starting scene without using Unity 6's render-pipeline assets.
        [MenuItem("Potion Panic/Create game scene")]
        public static void CreateScene()
        {
            if(!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var camera=new GameObject("Main Camera",typeof(Camera),typeof(AudioListener));
            camera.tag="MainCamera";camera.GetComponent<Camera>().clearFlags=CameraClearFlags.SolidColor;
            camera.GetComponent<Camera>().backgroundColor=new Color(.13f,.11f,.17f);
            new GameObject("Potion Panic Game").AddComponent<PotionPanicApp>();
            Directory.CreateDirectory("Assets/Scenes");
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(),ScenePath);
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(ScenePath,true)};
            AssetDatabase.SaveAssets();
        }
        [MenuItem("Potion Panic/Build Windows game")]
        public static void Windows()
        {
            PrepareMusic();
            if(!File.Exists(ScenePath))CreateScene();
            string output="Builds/Windows/Potion Panic.exe";
            string[] args=Environment.GetCommandLineArgs();
            for(int i=0;i<args.Length-1;i++)if(args[i]=="-potionOutput")output=args[i+1];
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            PlayerSettings.productName="Potion Panic";PlayerSettings.companyName="Little Moon";
            PlayerSettings.defaultScreenWidth=1440;PlayerSettings.defaultScreenHeight=900;
            PlayerSettings.resizableWindow=true;PlayerSettings.fullScreenMode=FullScreenMode.Windowed;
            PlayerSettings.runInBackground=true;
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64,false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64,new[]{UnityEngine.Rendering.GraphicsDeviceType.Direct3D11});
#if UNITY_6000_0_OR_NEWER
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone,ScriptingImplementation.Mono2x);
#else
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone,ScriptingImplementation.Mono2x);
#endif
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{
                scenes=new[]{ScenePath},locationPathName=output,target=BuildTarget.StandaloneWindows64,options=BuildOptions.None
            });
            if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Build failed: "+report.summary.result);
            Debug.Log("POTION PANIC BUILD SUCCEEDED with Unity "+Application.unityVersion+": "+output);
        }
    }
}
