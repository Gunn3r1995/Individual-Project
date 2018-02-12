using UnityEngine;
using UnityEditor;
using Assets.Scripts;

[CustomEditor (typeof(FieldOfView))]
public class FieldOfViewEditor : Editor {

    private void OnSceneGUI()
    {
        FieldOfView FOV = (FieldOfView)target;
        Handles.color = Color.white;
        Handles.DrawWireArc(FOV.transform.position, Vector3.up, Vector3.forward, 360, FOV.ViewRadius);
        Vector3 viewAngleA = FOV.DirFromAngle(-FOV.ViewAngle / 2, false);
        Vector3 viewAngleB = FOV.DirFromAngle(FOV.ViewAngle / 2, false);
        Handles.DrawLine(FOV.transform.position, FOV.transform.position + viewAngleA * FOV.ViewRadius);
		Handles.DrawLine(FOV.transform.position, FOV.transform.position + viewAngleB * FOV.ViewRadius);

        Handles.color = Color.red;
        foreach (Transform visibleTarget in FOV.VisibleTargets) {
            Handles.DrawLine(FOV.transform.position, visibleTarget.transform.position);
        }
    }
}
