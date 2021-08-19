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

#region ReSharper
// ReSharper disable CompareOfFloatsByEqualityOperator
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable InconsistentNaming
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Entities;
using UnityEngine;
using VisualPinball.Engine.Math;
using VisualPinball.Engine.VPT;
using VisualPinball.Engine.VPT.Primitive;

namespace VisualPinball.Unity
{
	[AddComponentMenu("Visual Pinball/Game Item/Primitive")]
	public class PrimitiveAuthoring : ItemMainRenderableAuthoring<Primitive, PrimitiveData>, IConvertGameObjectToEntity
	{
		#region Data

		public Vector3 Position = Vector3.zero;
		public Vector3 Rotation = Vector3.zero;
		public Vector3 Size = Vector3.one;

		public Vector3 Translation = Vector3.zero;

		public Vector3 ObjectRotation = Vector3.zero;

		public bool StaticRendering = true;

		#endregion

		public override ItemType ItemType => ItemType.Primitive;

		public override bool IsCollidable => !Data.IsToy;

		protected override Primitive InstantiateItem(PrimitiveData data) => new Primitive(data);
		protected override PrimitiveData InstantiateData() => new PrimitiveData();

		protected override Type MeshAuthoringType { get; } = typeof(ItemMeshAuthoring<Primitive, PrimitiveData, PrimitiveAuthoring>);
		protected override Type ColliderAuthoringType { get; } = typeof(ItemColliderAuthoring<Primitive, PrimitiveData, PrimitiveAuthoring>);

		public override IEnumerable<Type> ValidParents => PrimitiveColliderAuthoring.ValidParentTypes
			.Concat(PrimitiveMeshAuthoring.ValidParentTypes)
			.Distinct();

		public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
		{
			Convert(entity, dstManager);

			// register
			var primitive = GetComponent<PrimitiveAuthoring>().Item;
			transform.GetComponentInParent<Player>().RegisterPrimitive(primitive, entity, ParentEntity, gameObject);
		}

		public override void UpdateTransforms()
		{
			// scale matrix
			var scaleMatrix = new Matrix3D();
			scaleMatrix.SetScaling(Size.x, Size.y, Size.z);

			// translation matrix
			var tableHeight = TableAuthoring ? TableAuthoring.TableHeight : 0f;
			var transMatrix = new Matrix3D();
			transMatrix.SetTranslation(Position.x, Position.y, Position.z + tableHeight);

			// translation + rotation matrix
			var rotTransMatrix = new Matrix3D();
			rotTransMatrix.SetTranslation(Translation.x, Translation.y, Translation.z);

			var tempMatrix = new Matrix3D();
			tempMatrix.RotateZMatrix(MathF.DegToRad(Rotation.z));
			rotTransMatrix.Multiply(tempMatrix);
			tempMatrix.RotateYMatrix(MathF.DegToRad(Rotation.y));
			rotTransMatrix.Multiply(tempMatrix);
			tempMatrix.RotateXMatrix(MathF.DegToRad(Rotation.x));
			rotTransMatrix.Multiply(tempMatrix);

			tempMatrix.RotateZMatrix(MathF.DegToRad(ObjectRotation.z));
			rotTransMatrix.Multiply(tempMatrix);
			tempMatrix.RotateYMatrix(MathF.DegToRad(ObjectRotation.y));
			rotTransMatrix.Multiply(tempMatrix);
			tempMatrix.RotateXMatrix(MathF.DegToRad(ObjectRotation.x));
			rotTransMatrix.Multiply(tempMatrix);

			var fullMatrix = scaleMatrix.Clone();
			fullMatrix.Multiply(rotTransMatrix);
			fullMatrix.Multiply(transMatrix); // fullMatrix = Smatrix * RTmatrix * Tmatrix
			scaleMatrix.SetScaling(1.0f, 1.0f, 1.0f);
			fullMatrix.Multiply(scaleMatrix);

			transform.SetFromMatrix(fullMatrix.ToUnityMatrix());
		}

		public override IEnumerable<MonoBehaviour> SetData(PrimitiveData data, IMaterialProvider materialProvider, ITextureProvider textureProvider, Dictionary<string, IItemMainAuthoring> components)
		{
			var updatedComponents = new List<MonoBehaviour> { this };

			// transforms
			Position = data.Position.ToUnityVector3();
			Size = data.Size.ToUnityFloat3();
			Rotation = new Vector3(data.RotAndTra[0], data.RotAndTra[1], data.RotAndTra[2]);
			Translation = new Vector3(data.RotAndTra[3], data.RotAndTra[4], data.RotAndTra[5]);
			ObjectRotation = new Vector3(data.RotAndTra[6], data.RotAndTra[7], data.RotAndTra[8]);
			UpdateTransforms();

			// static rendering & visibility
			gameObject.SetActive(data.IsVisible);
			StaticRendering = data.StaticRendering;

			// mesh
			var meshComponent = GetComponent<PrimitiveMeshAuthoring>();
			if (meshComponent) {
				meshComponent.CreateMesh(data, textureProvider, materialProvider);

				updatedComponents.Add(meshComponent);
			}

			var collComponent = GetComponentInChildren<PrimitiveColliderAuthoring>();
			if (collComponent) {
				collComponent.enabled = !data.IsCollidable;

				collComponent.HitEvent = data.HitEvent;
				collComponent.Threshold = data.Threshold;
				collComponent.Elasticity = data.Elasticity;
				collComponent.ElasticityFalloff = data.ElasticityFalloff;
				collComponent.Friction = data.Friction;
				collComponent.Scatter = data.Scatter;
				collComponent.CollisionReductionFactor = data.CollisionReductionFactor;
				collComponent.OverwritePhysics = data.OverwritePhysics;

				updatedComponents.Add(collComponent);
			}

			return updatedComponents;
		}

		public override PrimitiveData CopyDataTo(PrimitiveData data, string[] materialNames, string[] textureNames)
		{
			// name and transforms
			data.Name = name;
			data.Position = Position.ToVertex3D();
			data.Size = Size.ToVertex3D();
			data.RotAndTra = new[] {
				Rotation.x, Rotation.y, Rotation.z,
				Translation.x, Translation.y, Translation.z,
				ObjectRotation.x, ObjectRotation.y, ObjectRotation.z,
			};

			// static rendering & visibility
			data.IsVisible = gameObject.activeInHierarchy;
			data.StaticRendering = StaticRendering;

			// materials
			var mr = GetComponent<MeshRenderer>();
			if (mr) {
				CopyMaterialName(mr, materialNames, textureNames, ref data.Material, ref data.Image, ref data.NormalMap);
			}

			// update collision
			// todo at some point we need to be able to toggle collidable during gameplay,
			// todo but for now let's keep things static.
			var collComponent = GetComponentInChildren<PrimitiveColliderAuthoring>();
			if (collComponent) {
				data.IsCollidable = collComponent.enabled;
				data.IsToy = false;

				data.HitEvent = collComponent.HitEvent;
				data.Threshold = collComponent.Threshold;
				data.Elasticity = collComponent.Elasticity;
				data.ElasticityFalloff = collComponent.ElasticityFalloff;
				data.Friction = collComponent.Friction;
				data.Scatter = collComponent.Scatter;
				data.CollisionReductionFactor = collComponent.CollisionReductionFactor;
				data.OverwritePhysics = collComponent.OverwritePhysics;

			} else {
				data.IsCollidable = false;
				data.IsToy = true;
			}

			return data;
		}

		public override void FillBinaryData()
		{
			var meshAuth = GetComponent<PrimitiveMeshAuthoring>();
			if (!meshAuth) {
				meshAuth = GetComponentInChildren<PrimitiveMeshAuthoring>();
			}

			var meshGo = meshAuth ? meshAuth.gameObject : gameObject;
			var mf = meshGo.GetComponent<MeshFilter>();
			if (mf) {
				Data.Mesh = mf.sharedMesh.ToVpMesh();
			}
		}

		#region Editor Tooling

		public override ItemDataTransformType EditorPositionType => ItemDataTransformType.ThreeD;
		public override Vector3 GetEditorPosition() => Position;
		public override void SetEditorPosition(Vector3 pos) => Position = pos;

		public override ItemDataTransformType EditorRotationType => ItemDataTransformType.ThreeD;
		public override Vector3 GetEditorRotation() => Rotation;
		public override void SetEditorRotation(Vector3 rot) => Rotation = rot;

		public override ItemDataTransformType EditorScaleType => ItemDataTransformType.ThreeD;
		public override Vector3 GetEditorScale() => Size;
		public override void SetEditorScale(Vector3 scale) => Size = scale;

		#endregion
	}
}
