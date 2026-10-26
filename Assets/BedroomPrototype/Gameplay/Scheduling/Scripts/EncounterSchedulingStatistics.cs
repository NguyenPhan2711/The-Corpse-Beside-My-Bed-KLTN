#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
public static class EncounterSchedulingStatistics
{
    [Serializable]public class Sample{public string name;public int seed,trials;public double[] weights;public int[] counts;public double[] percentages;public double chiSquare;public bool agrees;}
    public static Sample Simulate(string name,int trials,double[] weights,int? seed=null)
    {
        int actual=seed??Guid.NewGuid().GetHashCode();var rng=new System.Random(actual);var counts=new int[weights.Length];
        for(int i=0;i<trials;i++){int selected=BedroomEncounterSelection.Choose(weights,rng);if(selected>=0)counts[selected]++;}
        double total=weights.Sum(),chi=0;for(int i=0;i<weights.Length;i++){double expected=trials*weights[i]/total;if(expected>0)chi+=(counts[i]-expected)*(counts[i]-expected)/expected;}
        return new Sample{name=name,seed=actual,trials=trials,weights=weights,counts=counts,percentages=counts.Select(c=>100.0*c/trials).ToArray(),chiSquare=chi,agrees=chi<25&&counts.Where((c,i)=>weights[i]==0&&c!=0).Count()==0};
    }
    [UnityEditor.MenuItem("Bedroom Prototype/Debug/Simulate first encounter selections")]
    public static void RunMenu(){Run();}
    public static string Run()
    {
        var director=UnityEngine.Object.FindAnyObjectByType<BedroomEncounterDirector>();var config=director.CurrentSchedule;
        var cases=new List<Sample>{Simulate("Current configured closed-window first encounter",1000,new double[]{config.underBedWeight,config.doorWeight,config.windowWeight}),Simulate("Equal weights, fixed seed",1000,new double[]{1,1,1},12345),Simulate("Open window 3x",5000,new double[]{1,1,3},901),Simulate("Window excluded outside bed",2000,new double[]{1,1,0},902),Simulate("Under-bed on cooldown",2000,new double[]{0,1,1},903)};
        var repeat=Simulate("Fixed-seed replay",1000,new double[]{1,1,1},12345);bool reproducible=repeat.counts.SequenceEqual(cases[1].counts);
        for(int i=0;i<3;i++)cases.Add(Simulate("Independent normal seed "+(i+1),1000,new double[]{1,1,1}));
        string json=Newtonsoft.Json.JsonConvert.SerializeObject(new{passed=cases.All(c=>c.agrees)&&reproducible,order=new[]{"UnderBed","Door","Window"},reproducible,cases},Newtonsoft.Json.Formatting.Indented);
        Directory.CreateDirectory("Temp/BedroomPrototype");File.WriteAllText("Temp/BedroomPrototype/SchedulingStatistics.json",json);Debug.Log("[MonsterDirector simulation] "+json);return json;
    }
}
#endif
