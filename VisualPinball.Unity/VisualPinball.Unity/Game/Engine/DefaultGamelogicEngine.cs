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

using System;
using NLog;
using UnityEngine;
using Logger = NLog.Logger;

namespace VisualPinball.Unity
{
	[Serializable]
	public class DefaultGamelogicEngine : IGamelogicEngine, IGamelogicEngineWithSwitches, IGamelogicEngineWithCoils
	{
		public string Name => "Default Game Engine";

		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		private TableApi _tableApi;
		private BallManager _ballManager;

		private FlipperApi _leftFlipper;
		private FlipperApi _rightFlipper;

		public void OnInit(TableApi tableApi, BallManager ballManager)
		{
			_tableApi = tableApi;
			_ballManager = ballManager;

			// flippers
			_leftFlipper = _tableApi.Flipper("LeftFlipper")
			             ?? _tableApi.Flipper("FlipperLeft")
			             ?? _tableApi.Flipper("FlipperL")
			             ?? _tableApi.Flipper("LFlipper");
			_rightFlipper = _tableApi.Flipper("RightFlipper")
			             ?? _tableApi.Flipper("FlipperRight")
			             ?? _tableApi.Flipper("FlipperR")
			             ?? _tableApi.Flipper("RFlipper");

			// debug print stuff
			OnCoilChanged += DebugPrintCoil;
		}

		public void OnDestroy()
		{
			OnCoilChanged -= DebugPrintCoil;
		}

		public string[] AvailableSwitches { get; } = {"s_left_flipper", "s_right_flipper", "s_plunger", "s_create_ball"};

		public string[] AvailableCoils { get; } = {"c_left_flipper", "c_right_flipper", "c_auto_plunger"};

		public event EventHandler<CoilEventArgs> OnCoilChanged;

		public void Switch(string id, bool normallyClosed)
		{
			switch (id) {

				case "s_left_flipper":

					// todo remove when solenoids are done
					if (normallyClosed) {
						_leftFlipper?.RotateToEnd();
					} else {
						_leftFlipper?.RotateToStart();
					}
					OnCoilChanged?.Invoke(this, new CoilEventArgs("c_left_flipper", normallyClosed));
					break;

				case "s_right_flipper":

					// todo remove when solenoids are done
					if (normallyClosed) {
						_rightFlipper?.RotateToEnd();
					} else {
						_rightFlipper?.RotateToStart();
					}

					OnCoilChanged?.Invoke(this, new CoilEventArgs("c_right_flipper", normallyClosed));
					break;

				case "s_plunger":
					OnCoilChanged?.Invoke(this, new CoilEventArgs("c_auto_plunger", normallyClosed));
					break;

				case "s_create_ball": {
					if (normallyClosed) {
						_ballManager.CreateBall(new DebugBallCreator());
					}
					break;
				}
			}
		}


		private void DebugPrintCoil(object sender, CoilEventArgs e)
		{
			//Logger.Info("Coil {0} set to {1}.", e.Name, e.IsEnabled);
		}
	}
}