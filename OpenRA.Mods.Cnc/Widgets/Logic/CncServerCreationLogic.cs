#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Linq;
using System.Net;
using OpenRA.GameRules;
using OpenRA.Widgets;

namespace OpenRA.Mods.Cnc.Widgets.Logic
{
	public class CncServerCreationLogic
	{
		Widget panel;
		Action onCreate;
		Action onExit;
		Map map;
		bool advertiseOnline;

		[ObjectCreator.UseCtor]
		public CncServerCreationLogic([ObjectCreator.Param] Widget widget,
									  [ObjectCreator.Param] Action onExit,
									  [ObjectCreator.Param] Action openLobby)
		{
			panel = widget.GetWidget("CREATESERVER_PANEL");
			onCreate = openLobby;
			this.onExit = onExit;

			var settings = Game.Settings;
			panel.GetWidget<ButtonWidget>("BACK_BUTTON").OnClick = () => { Widget.CloseWindow(); onExit(); };
			panel.GetWidget<ButtonWidget>("CREATE_BUTTON").OnClick = CreateAndJoin;

			panel.GetWidget<ButtonWidget>("MAP_BUTTON").OnClick = () =>
			{
				Widget.OpenWindow("MAPCHOOSER_PANEL", new WidgetArgs()
				{
					{ "initialMap", map.Uid },
					{ "onExit", () => {} },
					{ "onSelect", (Action<Map>)(m => map = m) }
				});
			};

			map = Game.modData.AvailableMaps[ CncWidgetUtils.ChooseInitialMap(Game.Settings.Server.Map) ];

			panel.GetWidget<MapPreviewWidget>("MAP_PREVIEW").Map = () => map;
			panel.GetWidget<LabelWidget>("MAP_NAME").GetText = () => map.Title;

			panel.GetWidget<TextFieldWidget>("SERVER_NAME").Text = settings.Server.Name ?? "";
			panel.GetWidget<TextFieldWidget>("LISTEN_PORT").Text = settings.Server.ListenPort.ToString();
			advertiseOnline = Game.Settings.Server.AdvertiseOnline;

			var externalPort = panel.GetWidget<TextFieldWidget>("EXTERNAL_PORT");
			externalPort.Text = settings.Server.ExternalPort.ToString();
			externalPort.IsDisabled = () => !advertiseOnline;

			var advertiseCheckbox = panel.GetWidget<CheckboxWidget>("ADVERTISE_CHECKBOX");
			advertiseCheckbox.IsChecked = () => advertiseOnline;
			advertiseCheckbox.OnClick = () => advertiseOnline ^= true;

			// Disable these until we have some logic behind them
			panel.GetWidget<TextFieldWidget>("SERVER_DESC").IsDisabled = () => true;
			panel.GetWidget<TextFieldWidget>("SERVER_PASSWORD").IsDisabled = () => true;
		}

		void CreateAndJoin()
		{
			var name = panel.GetWidget<TextFieldWidget>("SERVER_NAME").Text;
			int listenPort, externalPort;
			if (!int.TryParse(panel.GetWidget<TextFieldWidget>("LISTEN_PORT").Text, out listenPort))
				listenPort = 1234;

			if (!int.TryParse(panel.GetWidget<TextFieldWidget>("EXTERNAL_PORT").Text, out externalPort))
				externalPort = 1234;

			// Save new settings
			Game.Settings.Server.Name = name;
			Game.Settings.Server.ListenPort = listenPort;
			Game.Settings.Server.ExternalPort = externalPort;
			Game.Settings.Server.AdvertiseOnline = advertiseOnline;
			Game.Settings.Server.Map = map.Uid;
			Game.Settings.Save();

			// Take a copy so that subsequent changes don't affect the server
			var settings = new ServerSettings(Game.Settings.Server);

			// Create and join the server
			Game.CreateServer(settings);
			Widget.CloseWindow();
			CncConnectingLogic.Connect(IPAddress.Loopback.ToString(), Game.Settings.Server.ListenPort, onCreate, onExit);
		}
	}
}
