﻿/**
 * <copyright>
 * Tracks and Rails Asset Package by Zen Fulcrum
 * Copyright 2015 Zen Fulcrum LLC
 * Usage is subject to Unity's Asset Store EULA (https://unity3d.com/legal/as_terms)
 * </copyright>
 */

namespace ZenFulcrum.Track {

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
#if UNITY_2018_3_OR_NEWER
	
#endif

/** Custom editor for tracks. */
[CustomEditor(typeof(Track), true)]
[ExecuteInEditMode]//this seems to be needed to add menu items
[CanEditMultipleObjects]
public class TrackEditor : Editor {

	public void OnEnable() {
		Undo.undoRedoPerformed += OnUndoRedo;
		PrefabUtility.prefabInstanceUpdated += OnPrefabUpdate;
	}

	public void OnDisable() {
		Undo.undoRedoPerformed -= OnUndoRedo;
		PrefabUtility.prefabInstanceUpdated -= OnPrefabUpdate;
	}

	//Collapsible sections:
	static bool showNavOptions = true;

	bool showTrackStartHandle, showTrackEndHandle;

	public static Track CreateNewTrack() {
		Transform parent = null;
		#if UNITY_2018_3_OR_NEWER
			var prefabStage = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
			if (prefabStage != null) parent = prefabStage.prefabContentsRoot.transform;
		#endif

		GameObject trackObj = new GameObject("Track");
		trackObj.transform.parent = parent;
		trackObj.AddComponent(typeof(MeshFilter));
		trackObj.AddComponent(typeof(MeshRenderer));
		trackObj.AddComponent(typeof(Track));

		//this must always be: (removing it will break things)
		trackObj.GetComponent<MeshFilter>().hideFlags = HideFlags.NotEditable;

		//do some reasonable defaults for the mesh:
		Track track = trackObj.GetComponent<Track>();

		string basePath = UnityEditor.AssetDatabase.GetAssetPath(MonoScript.FromMonoBehaviour(track));
		//get to the right folder
		basePath = Regex.Match(basePath, "^(.*)/[^/]+/[^/]+$").Groups[1].Value;
		//get all the assets there
		UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(basePath + "/Models/SimpleTrack.fbx");

		//find the ones we are interested in
		foreach (var asset in assets) {
			if (!(asset is Mesh)) continue;

			Mesh mesh = asset as Mesh;

			switch (mesh.name) {
				case "Rail":
					track.railMesh = mesh;
					break;
				case "Tie":
					track.tieMesh = mesh;
					break;
			}
		}

		//set the default material
		MeshRenderer mr = trackObj.GetComponent<MeshRenderer>();
		mr.sharedMaterial = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Material.mat");

		return track;
	}

	[UnityEditor.MenuItem("GameObject/3D Object/Track")]
	public static void AddNewTrack() {
		var track = CreateNewTrack();

		Undo.RegisterCreatedObjectUndo(track.gameObject, "Create Track");

		Selection.activeGameObject = track.gameObject;
	}

	override public void OnInspectorGUI() {
		DrawDefaultInspector();

		showNavOptions = EditorGUILayout.Foldout(showNavOptions, "Toolbox");
		if (showNavOptions) {
			if (serializedObject.isEditingMultipleObjects) RenderMultiSelectNavOptions();
			else RenderNavOptions();
		}

		//showEditorOptions = EditorGUILayout.Foldout(showEditorOptions, "Global Track Editing Options");
		//if (showEditorOptions) RenderEditorOptions();

		if (GUI.changed) {
			foreach (Object target in serializedObject.targetObjects) {
				if (target) MarkDirty(target as Track);
			}
		}
	}

	public void OnUndoRedo() {
		foreach (var t in targets) {
			var track = t as Track;
			if (!track) continue;
			MarkDirty(track);
			if (track.NextTrack) MarkDirty(track.NextTrack);
			if (track.PrevTrack) MarkDirty(track.PrevTrack);
		}


		//So. Let's say you have a track that was linked that we're undoing. And the mesh is now stale.
		//We have no way of knowing that it was involved in the undo since it's no longer linked and it's not in the
		//selection. Therefore, how do we go regen the mesh?
		//FIXYOU: This seems like an area for improvement in the editor APIs. Something like Object[] Undo.recentlyChanged.
		//Cope with the problem by using brute force on the entire scene on every undo/redo. :-(
		foreach (var piece in FindObjectsOfType<Track>()) {
			if (!piece.NextTrack || !piece.PrevTrack) {
				//In theory, this could have been affected by an undo...so make it regen.
				MarkDirty(piece);
			}
		}

		//Unity won't call Update for us if the track's disabled.
		DoUpdate(targets);
	}

	private void OnPrefabUpdate(GameObject obj) {
		var tracks = obj.GetComponentsInChildren<Track>();
		// ReSharper disable once CoVariantArrayConversion
		MarkDirty(tracks);
	}

	protected void RenderNavOptions() {
		Track track = target as Track;

		GUILayout.BeginVertical("box");

		GUILayout.Label("Navigation");

		//prev/next and create
		GUILayout.BeginHorizontal(GUIStyle.none);

		GUI.enabled = !(bool)track.PrevTrack;
		if (GUILayout.Button("+ Create Prev")) {
			Selection.activeGameObject = AddTrack(track, true).gameObject;
			Undo.RegisterCreatedObjectUndo(Selection.activeGameObject, "Create Track");
		}

		GUI.enabled = (bool)track.PrevTrack;
		if (GUILayout.Button(new GUIContent("< Select Prev", "Shift-click to add the piece to the selection."))) {
			if (Event.current.shift) {
				TrackEditorUtil.AddToSelection(track.PrevTrack.gameObject);
			} else {
				Selection.activeGameObject = track.PrevTrack.gameObject;
			}
		}
		GUI.enabled = (bool)track.NextTrack;
		if (GUILayout.Button(new GUIContent("Select Next >", "Shift-click to add the piece to the selection."))) {
			if (Event.current.shift) {
				TrackEditorUtil.AddToSelection(track.NextTrack.gameObject);
			} else {
				Selection.activeGameObject = track.NextTrack.gameObject;
			}
		}

		GUI.enabled = !(bool)track.NextTrack;
		if (GUILayout.Button("Create Next +")) {
			Selection.activeGameObject = AddTrack(track, false).gameObject;
			Undo.RegisterCreatedObjectUndo(Selection.activeGameObject, "Create Track");
		}

		GUI.enabled = true;
		GUILayout.EndHorizontal();


		//snap me/snap to me
		GUILayout.BeginHorizontal(GUIStyle.none);
		string what = track.PrevTrack ? "Prev" : "Nearest";

		if (GUILayout.Button("Snap " + what)) {
			FindAndSnap(track, false, false);
		}
		if (GUILayout.Button("Snap to " + what)) {
			FindAndSnap(track, true, false);
		}

		what = track.NextTrack ? "Next" : "Nearest";
		if (GUILayout.Button("Snap to " + what)) {
			FindAndSnap(track, true, true);
		}
		if (GUILayout.Button("Snap " + what)) {
			FindAndSnap(track, false, true);
		}
		GUILayout.EndHorizontal();

		GUILayout.BeginHorizontal();
		if (GUILayout.Button("Select Connected")) {
			TrackEditorUtil.SelectConnected(track);
		}
		GUILayout.EndHorizontal();


		GUILayout.EndVertical();
		GUILayout.BeginVertical("box");

		GUILayout.Label("Tools");

		GUILayout.BeginHorizontal(GUIStyle.none);
		{
			if (GUILayout.Button(TrackWindow.WindowName)) {
				TrackWindow.OpenIt();
			}

			DrawMirrorButton();

//			if (GUILayout.Button("Straight")) {
//				Undo.RecordObjects(GetObjectsInvolvedWithTrack(track), "Straighten");
//				var length = track.TrackEnd.position.magnitude;
//
//				var newEnd = new SimpleTransform(new Vector3(0, 0, length));
//				track.TrackEnd = newEnd;
//				if (track.NextTrack) track.NextTrack.TrackAbsoluteStart = track.TrackAbsoluteEnd;
//			}
//
//			if (GUILayout.Button("Short")) {
//				Undo.RecordObjects(GetObjectsInvolvedWithTrack(track), "Make short");
//				track.TrackEnd = new SimpleTransform(new Vector3(0, 0, 3));
//				if (track.NextTrack) track.NextTrack.TrackAbsoluteStart = track.TrackAbsoluteEnd;
//			}

//			if (GUILayout.Button("Ends Flat")) {
//				Undo.RecordObjects(GetObjectsInvolvedWithTrack(track), "Flatten");
//				var endPos = track.TrackAbsoluteEnd;
//				endPos.rotation = Quaternion.identity;
//				track.TrackAbsoluteEnd = endPos;
//				if (track.NextTrack) track.NextTrack.TrackAbsoluteStart = track.TrackAbsoluteEnd;
//			}

		}
		GUILayout.EndHorizontal();

		//Linking buttons
		GUILayout.BeginHorizontal(GUIStyle.none);

		if (GUILayout.Button("Split")) {
			SplitTrack();
		}

		if (GUILayout.Button("Unlink")) {
			Undo.RecordObjects(GetObjectsInvolvedWithTrack(track), "Unlink");

			if (track.PrevTrack) {
				var adj = track.PrevTrack;
				if (adj.PrevTrack == track) adj.PrevTrack = null;
				if (adj.NextTrack == track) adj.NextTrack = null;
			}
			if (track.NextTrack) {
				var adj = track.NextTrack;
				if (adj.PrevTrack == track) adj.PrevTrack = null;
				if (adj.NextTrack == track) adj.NextTrack = null;
			}

			track.PrevTrack = null;
			track.NextTrack = null;
		}
		
		if (GUILayout.Button("Regenerate")) {
			MarkDirty(track);
			DoUpdate(track);
		}

		GUILayout.EndHorizontal();

		GUILayout.EndVertical();

	}

	private void DrawMirrorButton() {
		if (GUILayout.Button(new GUIContent("Mirror Track", "Mirrors track along the global X-axis. (To get other mirror configurations, rotate, flip, rotate)"))) {
			MirrorSelection();
		}
	}

	public void MirrorSelection() {
		var objects = Selection.gameObjects;
		//todo: won't handle nested objects right always (if we alter parent and child)
		foreach (var obj in objects) {
			Undo.RecordObject(obj.transform, "Mirror");
			var p1 = new SimpleTransform(obj.transform);
			var p2 = new SimpleTransform();

			var track = obj.GetComponent<Track>();
			if (track) {
				Undo.RecordObject(track, "Mirror");
				p2 = track.TrackAbsoluteEnd;
			}

			p1.position.x *= -1;
			p1.rotation.y *= -1;
			p1.rotation.z *= -1;

			p2.position.x *= -1;
			p2.rotation.y *= -1;
			p2.rotation.z *= -1;

			obj.transform.position = p1.position;
			obj.transform.rotation = p1.rotation;

			if (track) {
				track.TrackAbsoluteEnd = p2;
			}
		}
	}

	protected void RenderMultiSelectNavOptions() {
		var firstTrack = Selection.activeGameObject.GetComponent<Track>();
		if (!firstTrack) return;//not a track.
		
		GUILayout.BeginVertical("box");
		GUILayout.Label("Navigation");

		GUILayout.BeginHorizontal(GUIStyle.none);
		{
			//Keep the spacing balanced when switching to multi-select
			GUI.enabled = false;
			GUILayout.Button("");

			GUI.enabled = firstTrack.PrevTrack;
			if (GUILayout.Button("< Select Prev")) {
				if (Event.current.shift) {
					TrackEditorUtil.AddToSelection(firstTrack.PrevTrack.gameObject);
				} else {
					Selection.activeGameObject = firstTrack.PrevTrack.gameObject;
				}
			}

			GUI.enabled = firstTrack.NextTrack;
			if (GUILayout.Button("Select Next >")) {
				if (Event.current.shift) {
					TrackEditorUtil.AddToSelection(firstTrack.NextTrack.gameObject);
				} else {
					Selection.activeGameObject = firstTrack.NextTrack.gameObject;
				}
			}

			GUI.enabled = false;
			GUILayout.Button("");

			GUI.enabled = true;
		}
		GUILayout.EndHorizontal();

		GUILayout.BeginHorizontal(GUIStyle.none);
		if (GUILayout.Button("Select Connected")) {
			//todolater: only selects the loop of the main selection, not all selected loops
			TrackEditorUtil.SelectConnected(firstTrack);
		}
		GUILayout.EndHorizontal();

		GUILayout.EndVertical();

		
		GUILayout.BeginVertical("box");
		GUILayout.Label("Tools");

		GUILayout.BeginHorizontal(GUIStyle.none);
		{
			var selectionOrder = TrackEditorUtil.IsContiguousSelection(serializedObject.targetObjects);
			GUI.enabled = selectionOrder != null;
			if (GUILayout.Button("Merge")) EditorApplication.delayCall += () => MergeTracks(selectionOrder);
			GUI.enabled = true;

			DrawMirrorButton();

			if (GUILayout.Button("Regenerate")) {
				MarkDirty(targets);
				DoUpdate(targets);
			}
		}
		GUILayout.EndHorizontal();

		GUILayout.EndVertical();
	}

	public void SplitTrack() {
		Track track = target as Track;
		Undo.RecordObjects(GetObjectsInvolvedWithTrack(track), "Split Track");

		//Record some info
		var endPos = track.TrackAbsoluteEnd;
		var nextTrack = track.NextTrack;
		var middle = track.Curve.GetPointAt(.5f).LocalToAbsolute(track.transform);


		var newPiece = AddTrack(track, false);
		Undo.RegisterCreatedObjectUndo(newPiece.gameObject, "Split Track");

		//Put stuff in the right place
		track.TrackAbsoluteEnd = middle;
		newPiece.TrackAbsoluteStart = middle;
		newPiece.TrackAbsoluteEnd = endPos;
		newPiece.NextTrack = nextTrack;

		//Update links to the next track, if any.
		if (nextTrack) {
			if (nextTrack.NextTrack == track) nextTrack.NextTrack = newPiece;
			if (nextTrack.PrevTrack == track) nextTrack.PrevTrack = newPiece;
		}

		//Turn this on, I bet they're about to use it.
		showTrackEndHandle = true;


	}


	/** Finds the nearest track in the given direction, then snaps to it as requested. */
	public void FindAndSnap(Track track, bool snapMe, bool snapEnd) {
		Track other = null;
		SimpleTransform myEdge;

		if (snapEnd) {
			other = track.NextTrack;
			myEdge = track.TrackAbsoluteEnd;
		} else {
			other = track.PrevTrack;
			myEdge = track.TrackAbsoluteStart;
		}

		if (!other) {
			//let's go looking for things since we're not currently linked
			float nearestPos = float.PositiveInfinity;
			foreach (Track sceneTrack in Object.FindObjectsOfType(typeof(Track))) {
				//skip ourself
				if (sceneTrack == track) continue;

				if (!sceneTrack.enabled) continue;

				//if they are connected to us unidirectionally, prefer completing that link instead of picking a new track
				if ((snapEnd && sceneTrack.PrevTrack == track) || (!snapEnd && sceneTrack.NextTrack == track)) {
					other = sceneTrack;
					break;
				}

				//skip items that are already linked to something on the end we would connect to
				if ((snapEnd && sceneTrack.PrevTrack) || (!snapEnd && sceneTrack.NextTrack)) {
					continue;
				}

				SimpleTransform stp;
				if (snapEnd) stp = sceneTrack.TrackAbsoluteStart;
				else stp = sceneTrack.TrackAbsoluteEnd;

				if (Vector3.Distance(myEdge.position, stp.position) < nearestPos) {
					other = sceneTrack;
					nearestPos = Vector3.Distance(myEdge.position, stp.position);
				}
			}
		}

		if (!other) {
			Debug.Log("Unable to find another track to snap to.");
			return;
		}

		//mark down undo for both itemst
		var objects = GetObjectsInvolvedWithTrack(track).Union(GetObjectsInvolvedWithTrack(other)).ToArray();
		Undo.RecordObjects(objects, "Snap Track");

		track.SnapTogether(other, snapMe, snapEnd);
		DoUpdate(objects);
	}

	/**
	 * Creates a new track that is like unto and connects to the end of the given track.
	 * Returns the track, does not log an Undo action.
	 */
	public static Track AddTrack(Track track, bool prevTrack) {
		//make a new track!

		SimpleTransform end;
		Track newTrack;
		var shiftEnd = false;
		if (prevTrack) {
			newTrack = GameObject.Instantiate(
				track,
				track.transform.position + track.transform.rotation * new Vector3(0, 0, -5),
				track.transform.rotation
			) as Track;
			//we need to snap to the prev track, but can't do that correctly uintil our transform parent is updated
			shiftEnd = true;

			//link them together (same-orientation)
			newTrack.NextTrack = track;
			track.PrevTrack = newTrack;
		} else {
			end = track.TrackAbsoluteEnd;
			newTrack = GameObject.Instantiate(track, end.position, end.rotation) as Track;
			//since we copied the track it will still have the same relative end, just from the new start

			//link them together (same-orientation)
			newTrack.PrevTrack = track;
			track.NextTrack = newTrack;
		}

		//put it in the same place in the tree
		newTrack.transform.parent = track.transform.parent;
		newTrack.transform.localScale = track.transform.localScale;
		newTrack.name = track.name;//make the name slightly less tacky than "Foobar(Clone)"

		if (shiftEnd) {
			newTrack.TrackAbsoluteEnd = new SimpleTransform(track.transform);
		}

		//make it generate its own mesh
		newTrack.Dirty = true;

		return newTrack;
	}

	protected static readonly Color curveColor = new Color(1, 1, .5f, .5f);
	protected static readonly Color directionArrowColor = new Color(.5f, .5f, .5f, .5f);
	protected static readonly Color strengthHandleColor = new Color32(0x89, 0x08, 0xB7, 0x80);


	public void OnSceneGUI() {
		var track = target as Track;

		if (Selection.objects.Length == 1) {
			//only show edit handles when it's the only selected object
			RenderEditHandles(track);
		}

		if (Event.current.type == EventType.Repaint) {
			//show direction for this track
			var p = track.Curve.GetPointAt(.5f).LocalToAbsolute(track.transform);
			var size = HandleUtility.GetHandleSize(p.position) * .3f;
			Handles.color = directionArrowColor;
			Handles.ConeHandleCap(0, p.position, p.rotation, size, EventType.Repaint);

			//show track path
			var prev = track.TrackAbsoluteStart.position;
			var steps = 40;
			Handles.color = curveColor;
			for (float i = 0; i < steps; i++) {
				var percent = (i + 1) / (float)steps;
				var pos = track.Curve.GetPointAt(percent).LocalToAbsolute(track.transform).position;

				Handles.DrawLine(prev, pos);

				prev = pos;
			}
		}

	}

	/** Returns an array of Objects that may be changed when changing the given Track. */
	public static Object[] GetObjectsInvolvedWithTrack(Track track) {
		List<Object> involvedTracks = new List<Object>();
		if (track.PrevTrack) {
			involvedTracks.Add(track.PrevTrack);
			involvedTracks.Add(track.PrevTrack.transform);
		}
		if (track.NextTrack) {
			involvedTracks.Add(track.NextTrack);
			involvedTracks.Add(track.NextTrack.transform);
		}
		involvedTracks.Add(track);
		involvedTracks.Add(track.transform);
		return involvedTracks.ToArray();
	}

	/**
	 * Returns a DrawCapFunction that will draw "scale" larger/smaller than given.
	 * Useful for making the hit box on an object bigger without drawing a larger object.
	 */
	protected static Handles.CapFunction ScaleDrawSize(float scale, Handles.CapFunction srcFunc) {
		return (id, pos, rotation, size, eventType) => srcFunc(id, pos, rotation, size * scale, eventType);
	}

	private Quaternion handleBaseRot, handleActionRot = Quaternion.identity;
	protected void RenderRotateMoveHandle(ref SimpleTransform where) {
		if (Tools.pivotRotation == PivotRotation.Global) {
			where.position = Handles.PositionHandle(where.position, Quaternion.identity);

			//Showing one thing with the handles while rotating differently needs some trickery:

			var baseEventType = Event.current.type;//record now as it may change to Used

			EditorGUI.BeginChangeCheck();
			//handleActionRot should be Quaternion.identity unless we are dragging.
			handleActionRot = Handles.RotationHandle(handleActionRot, where.position);

			switch (baseEventType) {
				case EventType.MouseDown:
					//record rotation when we started
					handleBaseRot = where.rotation;
					break;
				case EventType.MouseUp:
					//fixme: less than idea since you can hit esc and such, but good enough
					handleActionRot = Quaternion.identity;
					break;
			}

			if (EditorGUI.EndChangeCheck()) {
				//apply full rotation change to original value
				where.rotation = handleActionRot * handleBaseRot;
			}
		} else {
			//relative
			where.rotation = Handles.RotationHandle(where.rotation, where.position);
			where.position = Handles.PositionHandle(where.position, where.rotation);
		}
		where.MakeValid();
	}

	protected void RenderEditHandles(Track track) {
		var involvedTracks = GetObjectsInvolvedWithTrack(track);
		Undo.RecordObjects(involvedTracks, "Bend Track");

		const float handleSize = .35f;
		const float strengthScale = .25f;

		SimpleTransform t, tOrig;

		if (showTrackStartHandle) {
			//Handle rendering and inputs on the start transform of this node
			t = track.TrackAbsoluteStart.MakeValid();
			tOrig = new SimpleTransform(t);

			RenderRotateMoveHandle(ref t);

			if (t != tOrig) {
				track.TrackAbsoluteStart = t;

				//The adjacent track is "normally" linked, bring it with us.
				if (track.PrevTrack && track.PrevTrack.NextTrack == track) {
					track.PrevTrack.TrackAbsoluteEnd = t;
				}
			}

			//Handle rendering and inputs of the strength of this node
			var scale = track.Distance * strengthScale;
			var pos = t.position + t.forward * scale * track.curveStartStrength;
			Handles.color = curveColor;
			Handles.DrawLine(pos, t.position);
			Handles.color = strengthHandleColor;
			var updatedPos = Handles.Slider(
				pos, t.forward, HandleUtility.GetHandleSize(t.position) * 2,
				ScaleDrawSize(handleSize * .5f, Handles.SphereHandleCap), 0
			);
			if (updatedPos != pos) {
				track.curveStartStrength = Mathf.Max(0, Vector3.Dot(updatedPos - t.position, t.forward) / scale);
			}
		} else {
			//render "click to edit" button
			var p = track.TrackAbsoluteStart.MakeValid();
			var s = HandleUtility.GetHandleSize(p.position) * handleSize;
			Handles.color = Color.red;
			var clicked = Handles.Button(p.position, p.rotation, s, s, Handles.CylinderHandleCap);
			if (clicked) {
				showTrackStartHandle = true;
				if (Tools.pivotMode == PivotMode.Pivot) {
					//bump to center mode so the normal tool handles aren't at the same place
					Tools.pivotMode = PivotMode.Center;
				}
			}
		}


		if (showTrackEndHandle) {
			//Handle rendering and inputs on the end transform of this node
			t = track.TrackAbsoluteEnd.MakeValid();
			tOrig = new SimpleTransform(t);

			RenderRotateMoveHandle(ref t);

			if (t != tOrig) {
				track.TrackAbsoluteEnd = t;

				//The adjacent track is "normally" linked, bring it with us.
				if (track.NextTrack && track.NextTrack.PrevTrack == track) {
					track.NextTrack.TrackAbsoluteStart = t;
				}
			}

			//Handle rendering and inputs of the strength of this node
			var scale = track.Distance * strengthScale;
			var pos = t.position + t.backward * scale * track.curveEndStrength;
			Handles.color = curveColor;
			Handles.DrawLine(pos, t.position);
			Handles.color = strengthHandleColor;
			var updatedPos = Handles.Slider(
				pos, t.backward, HandleUtility.GetHandleSize(t.position) * 2,
				ScaleDrawSize(handleSize * .5f, Handles.SphereHandleCap), 0
			);
			if (updatedPos != pos) {
				track.curveEndStrength = Mathf.Max(0, Vector3.Dot(updatedPos - t.position, t.backward) / scale);
			}
		} else {
			//render "click to edit" button
			var p = track.TrackAbsoluteEnd.MakeValid();//Unity's definition of "Unit quaternion" is unnecessarily strict.
			var s = HandleUtility.GetHandleSize(p.position) * handleSize;
			Handles.color = Color.green;
			var clicked = Handles.Button(p.position, p.rotation, s, s, Handles.CylinderHandleCap);
			if (clicked) showTrackEndHandle = true;
		}

		//Draw navigation/add buttons
		System.Action<Track, bool> doNavButton = (adjTrack, start) => {
			var p = (start ? track.TrackAbsoluteStart : track.TrackAbsoluteEnd).MakeValid();
			var s = HandleUtility.GetHandleSize(p.position) * handleSize;
			var s2 = s * .7f;
			Handles.color = adjTrack ? new Color(0, 0, 1, .5f) : Color.green;

			//var direction = start ? p.Clone().AboutFace().rotation : p.rotation;
			var direction = p.rotation;
			var dirSign = start ? -1 : 1;

			var pos = p.position + (p.rotation * new Vector3(0, 0, dirSign * s * 4));
			var clicked = Handles.Button(pos, direction, s2, s2, Handles.ConeHandleCap);

			if (clicked && adjTrack) {
				Selection.activeGameObject = adjTrack.gameObject;
			} else if (clicked) {
				var newTrack = AddTrack(track, start);
				Undo.RegisterCreatedObjectUndo(newTrack.gameObject, "Create Track");
				Selection.activeGameObject = newTrack.gameObject;
			}
		};

		doNavButton(track.PrevTrack, true);
		doNavButton(track.NextTrack, false);


		if (GUI.changed) {
			MarkDirty(involvedTracks);
			DoUpdate(involvedTracks);
		}
	}

	/**
	 * Marks the mesh in need of updating.
	 */
	protected void MarkDirty(params Object[] objects) {
		foreach (Object obj in objects) {
			Track track = obj as Track;
			if (track) {
				track.Dirty = true;
			}
		}
	}

	/**
	 * Updates the mesh on the any given track pieces, if needed.
	 */
	private void DoUpdate(params Object[] objects) {
		foreach (Object obj in objects) {
			Track track = obj as Track;
			if (track) {
				track.Update();
			}
		}
	}

	/**
	 * Finds all the nearest tracks and links them together.
	 * Not fast. Only modifies this track, if this breaks links on other tracks, you will need to relink them too.
	 */
	public static void RelinkTrack(Track track) {
		if (!track) return;

		bool matchedNext = false, matchedPrev = false;


		SimpleTransform myStart = new SimpleTransform(track.transform), myEnd = track.TrackAbsoluteEnd;

		foreach (Track other in Object.FindObjectsOfType(typeof(Track))) {
			if (other == track) continue;
			SimpleTransform otherStart = new SimpleTransform(other.transform), otherEnd = other.TrackAbsoluteEnd;

			//join everything that can be joined
			if (Track.IsAligned(myStart, otherStart) || Track.IsAligned(myStart, otherEnd)) {
				matchedPrev = true;
				if (track.PrevTrack != other) {
					track.PrevTrack = other;
					Debug.Log("Linked " + other.name + " to before " + track.name);
				}
			}
			if (Track.IsAligned(myEnd, otherStart) || Track.IsAligned(myEnd, otherEnd)) {
				matchedNext = true;
				if (track.NextTrack != other) {
					track.NextTrack = other;
					Debug.Log("Linked " + other.name + " to after " + track.name);
				}
			}
		}

		if (!matchedNext)
			track.NextTrack = null;
		if (!matchedPrev)
			track.PrevTrack = null;
	}

	/** 
	 * Takes an ordered selection of tracks and merges them as one. 
	 * 
	 * If you are calling this from a GUI function, call it from EditorApplication.delayCall or else
	 * you'll get MissingReferenceException errors.
	 */
	public void MergeTracks(List<Track> tracks) {
		foreach (var track in tracks) {
			Undo.RecordObjects(GetObjectsInvolvedWithTrack(track), "Merge Track");
		}

		var first = tracks[0];
		var last = tracks[tracks.Count - 1];

		first.TrackAbsoluteEnd = last.TrackAbsoluteEnd;
		first.NextTrack = last.NextTrack;

		if (last.NextTrack) {
			var nextTrack = last.NextTrack;
			if (nextTrack.PrevTrack == last) nextTrack.PrevTrack = first;
			if (nextTrack.NextTrack == last) nextTrack.NextTrack = first;
		}

		foreach (var track in tracks.Skip(1)) {
			Undo.DestroyObjectImmediate(track.gameObject);
		}
	}

}

}
