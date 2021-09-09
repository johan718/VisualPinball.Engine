// Visual Pinball Engine
// Copyright (C) 2021 freezy and VPE Team
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

// ReSharper disable InconsistentNaming

using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using VisualPinball.Engine.Common;
using VisualPinball.Engine.Game;
using VisualPinball.Engine.Math;
using VisualPinball.Engine.VPT;
using VisualPinball.Engine.VPT.Primitive;
using VisualPinball.Engine.VPT.Table;
using VisualPinball.Unity.Playfield;

namespace VisualPinball.Unity
{
	[AddComponentMenu("Visual Pinball/Game Item/Playfield")]
	public class PlayfieldComponent : ItemMainRenderableComponent<TableData>
	{
		public static readonly Quaternion GlobalRotation = Quaternion.Euler(-90, 0, 0);
		public const float GlobalScale = 0.001f;

		#region Data

		public float TableHeight;

		public float GlassHeight;

		public float Left;

		public float Right = 952f;

		public float Top;

		public float Bottom = 2162f;

		public float AngleTiltMax = 6f;

		public float AngleTiltMin = 6f;

		public int PlayfieldDetailLevel = 10;

		[SerializeField] private string _playfieldImage;
		[SerializeField] private string _playfieldMaterial;

		#endregion

		public float Width => Right;
		public float Height => Bottom;

		public override ItemType ItemType => ItemType.Playfield;
		public override string ItemName => "Playfield";

		public override bool CanBeTransformed => false;

		public override TableData InstantiateData() => new TableData();

		protected override Type MeshAuthoringType => typeof(PlayfieldMeshComponent);
		protected override Type ColliderAuthoringType => typeof(PlayfieldColliderComponent);

		public override IEnumerable<Type> ValidParents => PlayfieldColliderComponent.ValidParentTypes
			.Concat(PlayfieldMeshComponent.ValidParentTypes)
			.Distinct();

		public Rect3D BoundingBox => new Rect3D(Left, Right, Top, Bottom, TableHeight, GlassHeight);

		public float3 Gravity {
			get {
				var tableComponent = GetComponentInParent<TableComponent>();
				var difficulty = tableComponent ? tableComponent.GlobalDifficulty : 0.2f;
				var slope = AngleTiltMin + (AngleTiltMax - AngleTiltMin) * difficulty;
				var strength = PhysicsConstants.DefaultTableGravity;
				return new float3(0, math.sin(math.radians(slope)) * strength, -math.cos(math.radians(slope)) * strength);
			}
		}

		private void Start()
		{
			GetComponentInParent<Player>().RegisterPlayfield(gameObject);
			var meshComp = GetComponentInChildren<PlayfieldMeshComponent>();
			if (meshComp) {
				World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<StaticNarrowPhaseSystem>().CollideAgainstPlayfieldPlane = meshComp.AutoGenerate;
			}
		}

		public override IEnumerable<MonoBehaviour> SetData(TableData data)
		{
			var updatedComponents = new List<MonoBehaviour> { this };

			// position
			TableHeight = data.TableHeight;
			GlassHeight = data.GlassHeight;
			Left = data.Left;
			Left = data.Left;
			Left = data.Left;
			Right = data.Right;
			Top = data.Top;
			Bottom = data.Bottom;
			AngleTiltMax = data.AngleTiltMax;
			AngleTiltMin = data.AngleTiltMin;

			// playfield material
			_playfieldImage = data.Image;
			_playfieldMaterial = data.PlayfieldMaterial;

			// collider data
			var collComponent = GetComponent<PlayfieldColliderComponent>();
			if (collComponent) {
				collComponent.Gravity = data.Gravity;
				collComponent.Elasticity = data.Elasticity;
				collComponent.ElasticityFalloff = data.ElasticityFalloff;
				collComponent.Friction = data.Friction;
				collComponent.Scatter = data.Scatter;
				collComponent.DefaultScatter = data.DefaultScatter;

				updatedComponents.Add(collComponent);
			}

			return updatedComponents;
		}

		public override IEnumerable<MonoBehaviour> SetReferencedData(TableData data, Table table, IMaterialProvider materialProvider, ITextureProvider textureProvider, Dictionary<string, IItemMainComponent> components)
		{
			var meshComponent = GetComponentInChildren<PlayfieldMeshComponent>();
			if (meshComponent && meshComponent.AutoGenerate) {
				meshComponent.CreateMesh(data, table, textureProvider, materialProvider);
			}
			return Array.Empty<MonoBehaviour>();
		}

		public IEnumerable<MonoBehaviour> SetReferencedData(PrimitiveData primitiveData, Table table, IMaterialProvider materialProvider, ITextureProvider textureProvider)
		{
			var mf = GetComponent<MeshFilter>();
			var playfieldMeshAuthoring = GetComponent<PlayfieldMeshComponent>();
			if (!mf || !playfieldMeshAuthoring) {
				return Array.Empty<MonoBehaviour>();
			}

			var updatedComponents = new List<MonoBehaviour> { this };
			var mg = new PrimitiveMeshGenerator(primitiveData);
			var ro = mg.GetRenderObject(table, primitiveData.Mesh, Origin.Original, false);
			ro.Material = new PbrMaterial(
				table.GetMaterial(_playfieldMaterial),
				table.GetTexture(_playfieldImage)
			);
			ro.Mesh.Transform(mg.TransformationMatrix(PlayfieldHeight)); // apply transformation to mesh, because this is the playfield
			ItemMeshComponent<PrimitiveData, PrimitiveComponent>.CreateMesh(gameObject, ro, "playfield_mesh", textureProvider, materialProvider);
			playfieldMeshAuthoring.AutoGenerate = false;

			updatedComponents.Add(playfieldMeshAuthoring);

			return updatedComponents;
		}

		public override TableData CopyDataTo(TableData data, string[] materialNames, string[] textureNames, bool forExport)
		{
			// position
			data.TableHeight = TableHeight;
			data.GlassHeight = GlassHeight;
			data.Left = Left;
			data.Right = Right;
			data.Top = Top;
			data.Bottom = Bottom;
			data.AngleTiltMax = AngleTiltMax;
			data.AngleTiltMin = AngleTiltMin;

			// playfield material
			data.Image = _playfieldImage;
			data.PlayfieldMaterial = _playfieldMaterial;

			// collider data
			var collComponent = GetComponent<PlayfieldColliderComponent>();
			if (collComponent) {
				data.Gravity = collComponent.Gravity;
				data.Elasticity = collComponent.Elasticity;
				data.ElasticityFalloff = collComponent.ElasticityFalloff;
				data.Friction = collComponent.Friction;
				data.Scatter = collComponent.Scatter;
				data.DefaultScatter = collComponent.DefaultScatter;
			}

			return data;
		}
	}
}