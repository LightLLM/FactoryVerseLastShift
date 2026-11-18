#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEngine;
namespace FactoryVerse {
public static class SceneBuilder {
 [MenuItem("FactoryVerse/Generate Last Shift Scene")]
 [InitializeOnLoadMethod]
 static void OnImport(){EditorApplication.delayCall+=EnsureMaterials;}
 static void EnsureMaterials(){Directory.CreateDirectory("Assets/FactoryVerse/Resources");string[] names={"FactoryVerseStandard","FactoryVerseRay"};string[] shaders={"Standard","Sprites/Default"};for(int i=0;i<names.Length;i++){var path="Assets/FactoryVerse/Resources/"+names[i]+".mat";if(!AssetDatabase.LoadAssetAtPath<Material>(path)){var shader=Shader.Find(shaders[i]);if(!shader)throw new Exception("Required shader missing: "+shaders[i]);AssetDatabase.CreateAsset(new Material(shader),path);}}AssetDatabase.SaveAssets();}
 public static void Build(){EnsureMaterials();Directory.CreateDirectory("Assets/FactoryVerse/Scenes");var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);new GameObject("FactoryVerse Bootstrap").AddComponent<StationBootstrap>();EditorSceneManager.SaveScene(scene,"Assets/FactoryVerse/Scenes/LastShift.unity");EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene("Assets/FactoryVerse/Scenes/LastShift.unity",true)};PlayerSettings.companyName="FactoryVerse";PlayerSettings.productName="FactoryVerse Last Shift";PlayerSettings.defaultScreenWidth=1280;PlayerSettings.defaultScreenHeight=720;PlayerSettings.fullScreenMode=FullScreenMode.Windowed;PlayerSettings.runInBackground=true;PlayerSettings.colorSpace=ColorSpace.Linear;AssetDatabase.SaveAssets();Debug.Log("FACTORYVERSE_SCENE_READY");}
 [MenuItem("FactoryVerse/Run Runtime Verification")]
 public static void Verify(){Build();Directory.CreateDirectory("Validation");File.WriteAllText("Validation/run-runtime-qa.flag","run");EditorApplication.isPlaying=true;}
 [MenuItem("FactoryVerse/Build Windows Player")]
 public static void BuildWindows(){Build();var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/FactoryVerse/Scenes/LastShift.unity"},locationPathName="Builds/Windows/FactoryVerse.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});Directory.CreateDirectory("Validation");File.WriteAllText("Validation/build-result.txt",report.summary.result+"\nWarnings: "+report.summary.totalWarnings+"\nErrors: "+report.summary.totalErrors);if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Player build failed");}
}}
#endif
