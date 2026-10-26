using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public sealed class CorpseGlassReveal:MonoBehaviour
{
 public SkinnedMeshRenderer[] sourceGlass;public Transform fragmentRoot,face;public MeshRenderer[] fragments;public Light revealFill;public SkinnedMeshRenderer corpseIdentity,playerIdentity;
 [Min(.1f)]public float fractureSeconds=.6f,revealSeconds=1.8f,observeSeconds=.8f;public int night2RemovedPieces=3,night3RemovedPieces=6;public float fragmentFall=.5f;
 public bool Finished{get;private set;}public bool Cracked{get;private set;}public bool Ready{get;private set;}
 Vector3[] home;Quaternion[] homeRotation;readonly List<Mesh> meshes=new List<Mesh>();
 public bool SharedFacialIdentity=>corpseIdentity&&playerIdentity&&corpseIdentity.sharedMesh==playerIdentity.sharedMesh;
 IEnumerator Start(){yield return null;yield return null;BuildFragments();ResetForNight(GetComponent<CorpseEncounterController>().phases.tasks.CurrentDay);}
 void OnDestroy(){foreach(var mesh in meshes)if(mesh)Destroy(mesh);}
 public void BuildFragments()
 {
  if(Ready)return;int count=fragments.Length;if(count==0)return;
  var baked=new List<Mesh>();var matrices=new List<Matrix4x4>();var materials=new List<Material>();float minZ=float.MaxValue,maxZ=float.MinValue;
  foreach(var source in sourceGlass){var mesh=new Mesh();source.BakeMesh(mesh);baked.Add(mesh);matrices.Add(fragmentRoot.worldToLocalMatrix*source.transform.localToWorldMatrix);materials.AddRange(source.sharedMaterials);foreach(var v in mesh.vertices){var p=matrices[matrices.Count-1].MultiplyPoint3x4(v);minZ=Mathf.Min(minZ,p.z);maxZ=Mathf.Max(maxZ,p.z);}}
  var vertices=new List<Vector3>[count];var triangles=new List<int>[count][];for(int i=0;i<count;i++){vertices[i]=new List<Vector3>();triangles[i]=new List<int>[materials.Count];for(int j=0;j<materials.Count;j++)triangles[i][j]=new List<int>();}
  int materialOffset=0;for(int s=0;s<baked.Count;s++){var mesh=baked[s];var vs=mesh.vertices;for(int sub=0;sub<mesh.subMeshCount;sub++){var indices=mesh.GetTriangles(sub);for(int t=0;t<indices.Length;t+=3){var a=matrices[s].MultiplyPoint3x4(vs[indices[t]]);var b=matrices[s].MultiplyPoint3x4(vs[indices[t+1]]);var c=matrices[s].MultiplyPoint3x4(vs[indices[t+2]]);var center=(a+b+c)/3;int slice=Mathf.Clamp(Mathf.FloorToInt((center.z-minZ)/Mathf.Max(.001f,maxZ-minZ)*(count/2)),0,count/2-1);int part=slice*2+(center.x>=0?1:0);int start=vertices[part].Count;vertices[part].Add(a);vertices[part].Add(b);vertices[part].Add(c);triangles[part][materialOffset+sub].Add(start);triangles[part][materialOffset+sub].Add(start+1);triangles[part][materialOffset+sub].Add(start+2);}}materialOffset+=mesh.subMeshCount;}
  home=new Vector3[count];homeRotation=new Quaternion[count];for(int i=0;i<count;i++){var mesh=new Mesh{name="Runtime corpse glass fragment "+i};var bounds=new Bounds();if(vertices[i].Count>0){bounds=new Bounds(vertices[i][0],Vector3.zero);foreach(var v in vertices[i])bounds.Encapsulate(v);}var centered=new List<Vector3>();foreach(var v in vertices[i])centered.Add(v-bounds.center);mesh.SetVertices(centered);mesh.subMeshCount=materials.Count;for(int sub=0;sub<materials.Count;sub++)mesh.SetTriangles(triangles[i][sub],sub);mesh.RecalculateNormals();mesh.RecalculateBounds();fragments[i].GetComponent<MeshFilter>().sharedMesh=mesh;fragments[i].sharedMaterials=materials.ToArray();fragments[i].transform.localPosition=bounds.center;fragments[i].transform.localRotation=Quaternion.identity;home[i]=bounds.center;homeRotation[i]=Quaternion.identity;meshes.Add(mesh);}
  foreach(var mesh in baked)Destroy(mesh);Ready=true;
 }
 void HideOriginal(){foreach(var r in sourceGlass)if(r)r.enabled=false;}
 public void ResetForNight(int night)
 {
  if(revealFill)revealFill.enabled=false;if(!Ready)BuildFragments();Finished=false;Cracked=false;if(!Ready)return;HideOriginal();int removed=night>=3?night3RemovedPieces:night==2?night2RemovedPieces:0;
  for(int i=0;i<fragments.Length;i++){fragments[i].transform.localPosition=home[i];fragments[i].transform.localRotation=homeRotation[i];fragments[i].enabled=i>=removed;}
 }
 public void ApplyCompleted(){if(revealFill)revealFill.enabled=false;if(!Ready)BuildFragments();Finished=true;Cracked=true;HideOriginal();foreach(var r in fragments)if(r)r.enabled=false;}
 public IEnumerator Fracture()
 {
  Cracked=true;float t=0;while(t<fractureSeconds){t+=Time.unscaledDeltaTime;for(int i=0;i<fragments.Length;i++)if(fragments[i].enabled)fragments[i].transform.localPosition=home[i]+new Vector3(Mathf.Sin(t*65+i)*.004f,0,0);yield return null;}
  int removed=0;for(int i=0;i<fragments.Length&&removed<2;i++)if(fragments[i].enabled){fragments[i].enabled=false;removed++;}
 }
 public IEnumerator Reveal(BedroomPhaseController phases)
 {
  if(!Ready)BuildFragments();HideOriginal();if(revealFill){revealFill.transform.position=face.position+new Vector3(.15f,.3f,.1f);revealFill.enabled=true;}float t=0;while(t<revealSeconds){t+=Time.unscaledDeltaTime;float p=Mathf.Clamp01(t/Mathf.Max(.1f,revealSeconds));phases.SetNarrativeFocus(face,Mathf.SmoothStep(0,1,p*2));for(int i=0;i<fragments.Length;i++)if(fragments[i].enabled){float fall=Mathf.Clamp01((p-i*.025f)*1.5f);fragments[i].transform.localPosition=home[i]+new Vector3((i%2==0?-.12f:.12f)*fall,-fragmentFall*fall,0);fragments[i].transform.localRotation=homeRotation[i]*Quaternion.Euler(fall*(60+i*5),fall*35,fall*(i%2==0?40:-40));if(fall>=1)fragments[i].enabled=false;}yield return null;}
  ApplyCompleted();if(revealFill)revealFill.enabled=true;yield return new WaitForSecondsRealtime(observeSeconds);t=0;while(t<.4f){t+=Time.unscaledDeltaTime;phases.SetNarrativeFocus(face,1-Mathf.Clamp01(t/.4f));yield return null;}phases.SetNarrativeFocus(null,0);if(revealFill)revealFill.enabled=false;
 }
}

