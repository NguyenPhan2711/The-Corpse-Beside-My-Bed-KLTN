using System;
using System.Collections.Generic;
// Shared by the runtime director and the fast statistical utility. No global Unity random state is modified.
public static class BedroomEncounterSelection
{
    public static int Choose(IReadOnlyList<double> weights, Random random)
    {
        double sum=0;for(int i=0;i<weights.Count;i++)if(weights[i]>0&&!double.IsNaN(weights[i])&&!double.IsInfinity(weights[i]))sum+=weights[i];
        if(sum<=0)return -1;
        double roll=random.NextDouble()*sum;
        for(int i=0;i<weights.Count;i++){double weight=weights[i];if(weight<=0||double.IsNaN(weight)||double.IsInfinity(weight))continue;roll-=weight;if(roll<0)return i;}
        for(int i=weights.Count-1;i>=0;i--)if(weights[i]>0)return i;return -1;
    }
}
