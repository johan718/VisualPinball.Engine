﻿// Visual Pinball Engine
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

using System;
using System.Collections.Generic;
using Unity.Entities;
using VisualPinball.Engine.VPT.Table;

namespace VisualPinball.Unity
{
	public class TriggerApi : ItemApi<Engine.VPT.Trigger.Trigger, Engine.VPT.Trigger.TriggerData>,
		IApiInitializable, IApiHittable, IApiSwitch, IApiColliderGenerator
	{
		/// <summary>
		/// Event emitted when the table is started.
		/// </summary>
		public event EventHandler Init;

		/// <summary>
		/// Event emitted when the ball glides on the trigger.
		/// </summary>
		public event EventHandler<HitEventArgs> Hit;

		/// <summary>
		/// Event emitted when the ball leaves the trigger.
		/// </summary>
		public event EventHandler<HitEventArgs> UnHit;

		/// <summary>
		/// Event emitted when the trigger is switched on or off.
		/// </summary>
		public event EventHandler<SwitchEventArgs> Switch;

		internal TriggerApi(Engine.VPT.Trigger.Trigger item, Entity entity, Entity parentEntity, Player player) : base(item, entity, parentEntity, player)
		{
		}

		IApiSwitchStatus IApiSwitch.AddSwitchDest(SwitchConfig switchConfig) => AddSwitchDest(switchConfig.WithPulse(Item.IsPulseSwitch));
		void IApiSwitch.AddWireDest(WireDestConfig wireConfig) => AddWireDest(wireConfig.WithPulse(Item.IsPulseSwitch));
		void IApiSwitch.RemoveWireDest(string destId) => RemoveWireDest(destId);
		void IApiSwitch.DestroyBall(Entity ballEntity) => DestroyBall(ballEntity);

		#region Collider Generation

		protected override bool FireHitEvents { get; } = true;
		Entity IApiColliderGenerator.ColliderEntity => Entity;

		void IApiColliderGenerator.CreateColliders(Table table, List<ICollider> colliders)
		{
			var colliderGenerator = new TriggerColliderGenerator(this);
			colliderGenerator.GenerateColliders(table, colliders);
		}

		ColliderInfo IApiColliderGenerator.GetColliderInfo() => GetColliderInfo();

		#endregion


		#region Events

		void IApiInitializable.OnInit(BallManager ballManager)
		{
			base.OnInit(ballManager);
			Init?.Invoke(this, EventArgs.Empty);
		}

		void IApiHittable.OnHit(Entity ballEntity, bool isUnHit)
		{
			if (isUnHit) {
				UnHit?.Invoke(this, new HitEventArgs(ballEntity));
				Switch?.Invoke(this, new SwitchEventArgs(false, ballEntity));
				OnSwitch(false);

			} else {
				Hit?.Invoke(this, new HitEventArgs(ballEntity));
				Switch?.Invoke(this, new SwitchEventArgs(true, ballEntity));
				OnSwitch(true);
			}
		}

		#endregion
	}
}
