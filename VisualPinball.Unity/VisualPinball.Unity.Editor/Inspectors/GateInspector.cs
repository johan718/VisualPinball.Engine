﻿// Visual Pinball Engine
// Copyright (C) 2020 freezy and VPE Team
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program. If not, see <https://www.gnu.org/licenses/>.

using UnityEditor;
using VisualPinball.Engine.VPT;

namespace VisualPinball.Unity.Editor
{
	[CustomEditor(typeof(GateAuthoring))]
	public class GateInspector : ItemInspector
	{
		private GateAuthoring _gate;
		private bool _foldoutColorsAndFormatting = true;
		private bool _foldoutPosition = true;
		private bool _foldoutPhysics = true;
		private bool _foldoutMisc = true;

		private static string[] _gateTypeStrings = { "Wire: 'W'", "Wire: Rectangle", "Plate", "Long Plate" };
		private static int[] _gateTypeValues = { GateType.GateWireW, GateType.GateWireRectangle, GateType.GatePlate, GateType.GateLongPlate };

		protected override void OnEnable()
		{
			base.OnEnable();
			_gate = target as GateAuthoring;
		}

		protected virtual void OnSceneGUI()
		{
			if (target is IEditableItemAuthoring editable) {
				var position = editable.GetEditorPosition();
				var transform = (target as MonoBehaviour).transform;
				if (transform != null && transform.parent != null) {
					position = transform.parent.TransformPoint(position);
					var axis = transform.TransformDirection(Vector3.up);
					var scale = _gate.Item.Data.Length * 0.0005f;
					Handles.color = Color.white;
					Handles.DrawWireDisc(position, axis, scale);
					Color col = Color.grey;
					col.a = 0.25f;
					Handles.color = col;
					Handles.DrawSolidDisc(position, axis, scale);

					Handles.color = Color.white;
					var arrowscale = 0.048f + Mathf.PingPong(Time.realtimeSinceStartup * 0.005f, 0.002f);
					Handles.ArrowHandleCap(-1, position, Quaternion.LookRotation(-axis), arrowscale, EventType.Repaint);
					if (_gate.Item.Data.TwoWay) {
						Handles.ArrowHandleCap(-1, position, Quaternion.LookRotation(axis), arrowscale, EventType.Repaint);
					}
				}
				HandleUtility.Repaint();
			}
		}
		public override void OnInspectorGUI()
		{
			OnPreInspectorGUI();

			if (_foldoutColorsAndFormatting = EditorGUILayout.BeginFoldoutHeaderGroup(_foldoutColorsAndFormatting, "Colors & Formatting")) {
				DropDownField("Type", ref _gate.data.GateType, _gateTypeStrings, _gateTypeValues);
				ItemDataField("Visible", ref _gate.data.IsVisible);
				ItemDataField("Show Bracket", ref _gate.data.ShowBracket);
				MaterialField("Material", ref _gate.data.Material);
			}
			EditorGUILayout.EndFoldoutHeaderGroup();

			if (_foldoutPosition = EditorGUILayout.BeginFoldoutHeaderGroup(_foldoutPosition, "Position")) {
				ItemDataField("", ref _gate.data.Center);
				ItemDataField("Length", ref _gate.data.Length);
				ItemDataField("Height", ref _gate.data.Height);
				ItemDataField("Rotation", ref _gate.data.Rotation);
				ItemDataField("Open Angle", ref _gate.data.AngleMax, dirtyMesh: false);
				ItemDataField("Close Angle", ref _gate.data.AngleMin, dirtyMesh: false);
				SurfaceField("Surface", ref _gate.data.Surface);
			}
			EditorGUILayout.EndFoldoutHeaderGroup();

			if (_foldoutPhysics = EditorGUILayout.BeginFoldoutHeaderGroup(_foldoutPhysics, "Physics")) {
				ItemDataField("Elasticity", ref _gate.data.Elasticity, dirtyMesh: false);
				ItemDataField("Friction", ref _gate.data.Friction, dirtyMesh: false);
				ItemDataField("Damping", ref _gate.data.Damping, dirtyMesh: false);
				ItemDataField("Gravity Factor", ref _gate.data.GravityFactor, dirtyMesh: false);
				ItemDataField("Collidable", ref _gate.data.IsCollidable, dirtyMesh: false);
				ItemDataField("Two Way", ref _gate.data.TwoWay, dirtyMesh: false);
			}
			EditorGUILayout.EndFoldoutHeaderGroup();

			if (_foldoutMisc = EditorGUILayout.BeginFoldoutHeaderGroup(_foldoutMisc, "Misc")) {
				ItemDataField("Timer Enabled", ref _gate.data.IsTimerEnabled, dirtyMesh: false);
				ItemDataField("Timer Interval", ref _gate.data.TimerInterval, dirtyMesh: false);
			}
			EditorGUILayout.EndFoldoutHeaderGroup();

			base.OnInspectorGUI();
		}

		protected override void FinishEdit(string label, bool dirtyMesh = true)
		{
			if (label == "Two Way") {
				SceneView.RepaintAll();
			}
			base.FinishEdit(label, dirtyMesh);
		}

	}
}
