using HarmonyLib;
using ProtoBuf;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game;
using Sandbox.Game.Gui;
using Sandbox.Game.GUI;
using Sandbox.Game.Localization;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.SessionComponents;
using Sandbox.Game.VoiceChat;
using Sandbox.Game.World;
using Sandbox.Graphics.GUI;
using SeamlessClientPlugin.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using VRage;
using VRage.Audio;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.Utils;
using VRageMath;

namespace SeamlessClientPlugin.Utilities
{

    public class OnlinePlayers
    {
        private static Harmony Patcher = new Harmony("OnlinePlayersPatcher");
        public static List<OnlineServer> AllServers = new List<OnlineServer>();
        public static int currentServer;
        private static string _currentServerName;

        public static int totalPlayerCount = 0;
        public static int currentPlayerCount = 0;


        private static MethodInfo m_UpdateCaption;
        private static MethodInfo m_RefreshMuteIcons;
        private static MethodInfo m_OnToggleMutePressed;
        private static MethodInfo m_AddCaption;


        private static MethodInfo m_profileButton_ButtonClicked;
        private static MethodInfo m_promoteButton_ButtonClicked;
        private static MethodInfo m_demoteButton_ButtonClicked;
        private static MethodInfo m_kickButton_ButtonClicked;
        private static MethodInfo m_banButton_ButtonClicked;
        private static MethodInfo m_tradeButton_ButtonClicked;
        private static MethodInfo m_inviteButton_ButtonClicked;
        private static MethodInfo m_PlayersTable_ItemSelected;

        private static MethodInfo m_UpdateButtonsEnabledState;


        private static FieldInfo m_playersTable;
        private static FieldInfo m_pings;
        private static FieldInfo m_PlayersTable;
        private static FieldInfo m_MaxPlayers;
        private static FieldInfo m_Warfare_timeRemainting_label;
        private static FieldInfo m_Warfare_timeRemainting_time;
        private static FieldInfo m_LastSelected;

        /* Buttons */
        private static FieldInfo m_ProfileButton;
        private static FieldInfo m_PromoteButton;
        private static FieldInfo m_DemoteButton;
        private static FieldInfo m_KickButton;
        private static FieldInfo m_BanButton;
        private static FieldInfo m_TradeButton;
        private static FieldInfo m_InviteButton;

        private static FieldInfo m_caption;
        private static FieldInfo m_LobbyTypeCombo;
        private static FieldInfo m_MaxPlayersSlider;



        public static void Patch()
        {
            m_playersTable = typeof(MyGuiScreenPlayers).GetField("m_playersTable", BindingFlags.Instance | BindingFlags.NonPublic);
            m_pings = typeof(MyGuiScreenPlayers).GetField("pings", BindingFlags.Instance | BindingFlags.NonPublic);
            m_UpdateCaption = typeof(MyGuiScreenPlayers).GetMethod("UpdateCaption", BindingFlags.Instance | BindingFlags.NonPublic);
            m_RefreshMuteIcons = typeof(MyGuiScreenPlayers).GetMethod("RefreshMuteIcons", BindingFlags.Instance | BindingFlags.NonPublic);
            m_OnToggleMutePressed = typeof(MyGuiScreenPlayers).GetMethod("OnToggleMutePressed", BindingFlags.Instance | BindingFlags.NonPublic);


            m_profileButton_ButtonClicked = typeof(MyGuiScreenPlayers).GetMethod("profileButton_ButtonClicked", BindingFlags.Instance | BindingFlags.NonPublic);
            m_promoteButton_ButtonClicked = typeof(MyGuiScreenPlayers).GetMethod("promoteButton_ButtonClicked", BindingFlags.Instance | BindingFlags.NonPublic);
            m_demoteButton_ButtonClicked = typeof(MyGuiScreenPlayers).GetMethod("demoteButton_ButtonClicked", BindingFlags.Instance | BindingFlags.NonPublic);
            m_kickButton_ButtonClicked = typeof(MyGuiScreenPlayers).GetMethod("kickButton_ButtonClicked", BindingFlags.Instance | BindingFlags.NonPublic);
            m_banButton_ButtonClicked = typeof(MyGuiScreenPlayers).GetMethod("banButton_ButtonClicked", BindingFlags.Instance | BindingFlags.NonPublic);
            m_tradeButton_ButtonClicked = typeof(MyGuiScreenPlayers).GetMethod("tradeButton_ButtonClicked", BindingFlags.Instance | BindingFlags.NonPublic);
            m_inviteButton_ButtonClicked = typeof(MyGuiScreenPlayers).GetMethod("inviteButton_ButtonClicked", BindingFlags.Instance | BindingFlags.NonPublic);
            m_UpdateButtonsEnabledState = typeof(MyGuiScreenPlayers).GetMethod("UpdateButtonsEnabledState", BindingFlags.Instance | BindingFlags.NonPublic);
            m_PlayersTable_ItemSelected = typeof(MyGuiScreenPlayers).GetMethod("playersTable_ItemSelected", BindingFlags.Instance | BindingFlags.NonPublic);

            //m_SetColumnName = typeof(MyGuiScreenPlayers).GetMethod("SetColumnName", BindingFlags.Instance | BindingFlags.se);





            m_caption = typeof(MyGuiScreenPlayers).GetField("m_caption", BindingFlags.Instance | BindingFlags.NonPublic);
            m_PlayersTable = typeof(MyGuiScreenPlayers).GetField("m_playersTable", BindingFlags.Instance | BindingFlags.NonPublic);
            m_MaxPlayers = typeof(MyGuiScreenPlayers).GetField("m_maxPlayers", BindingFlags.Instance | BindingFlags.NonPublic);
            m_Warfare_timeRemainting_label = typeof(MyGuiScreenPlayers).GetField("m_warfare_timeRemainting_label", BindingFlags.Instance | BindingFlags.NonPublic);
            m_Warfare_timeRemainting_time = typeof(MyGuiScreenPlayers).GetField("m_warfare_timeRemainting_time", BindingFlags.Instance | BindingFlags.NonPublic);
            m_LastSelected = typeof(MyGuiScreenPlayers).GetField("m_lastSelected", BindingFlags.Instance | BindingFlags.NonPublic);
            m_MaxPlayersSlider = typeof(MyGuiScreenPlayers).GetField("m_maxPlayersSlider", BindingFlags.Instance | BindingFlags.NonPublic);


            /* Buttons */
            m_ProfileButton = typeof(MyGuiScreenPlayers).GetField("m_profileButton", BindingFlags.Instance | BindingFlags.NonPublic);
            m_PromoteButton = typeof(MyGuiScreenPlayers).GetField("m_promoteButton", BindingFlags.Instance | BindingFlags.NonPublic);
            m_DemoteButton = typeof(MyGuiScreenPlayers).GetField("m_demoteButton", BindingFlags.Instance | BindingFlags.NonPublic);
            m_KickButton = typeof(MyGuiScreenPlayers).GetField("m_kickButton", BindingFlags.Instance | BindingFlags.NonPublic);
            m_BanButton = typeof(MyGuiScreenPlayers).GetField("m_banButton", BindingFlags.Instance | BindingFlags.NonPublic);
            m_TradeButton = typeof(MyGuiScreenPlayers).GetField("m_tradeButton", BindingFlags.Instance | BindingFlags.NonPublic);
            m_InviteButton = typeof(MyGuiScreenPlayers).GetField("m_inviteButton", BindingFlags.Instance | BindingFlags.NonPublic);
            m_LobbyTypeCombo = typeof(MyGuiScreenPlayers).GetField("m_lobbyTypeCombo", BindingFlags.Instance | BindingFlags.NonPublic);
            m_AddCaption = typeof(MyGuiScreenPlayers).GetMethod("AddCaption", BindingFlags.Instance | BindingFlags.NonPublic, null, new Type[] { typeof(MyStringId), typeof(Vector4?), typeof(Vector2?), typeof(float) }, null);








            MethodInfo recreateControls = typeof(MyGuiScreenPlayers).GetMethod("RecreateControls", BindingFlags.Instance | BindingFlags.Public);
            MethodInfo updateCaption = typeof(MyGuiScreenPlayers).GetMethod("UpdateCaption", BindingFlags.Instance | BindingFlags.NonPublic);




            Patcher.Patch(recreateControls, prefix: new HarmonyMethod(GetPatchMethod(nameof(RecreateControlsPrefix))));
            Patcher.Patch(updateCaption, prefix: new HarmonyMethod(GetPatchMethod(nameof(UpdateCaption))));
            //Patcher.Patch(recreateControls, postfix: new HarmonyMethod(GetPatchMethod(nameof(RecreateControlsSuffix))));
        }


        public static bool RecreateControlsPrefix(MyGuiScreenPlayers __instance, bool constructor)
        {

            if (MyMultiplayer.Static != null && MyMultiplayer.Static.IsLobby)
            {
                return true;
            }

            try
            {


                __instance.Controls.Clear();
                __instance.Elements.Clear();
                //__instance.Elements.Add(m_cl);
                __instance.FocusedControl = null;
                //__instance.m_firstUpdateServed = false;
                //__instance.m_screenCreation = DateTime.UtcNow;
                //__instance.m_gamepadHelpInitialized = false;
                //__instance.m_gamepadHelpLabel = null;


                //SeamlessClient.TryShow("A");


                //__instance.RecreateControls(constructor);
                __instance.Size = new Vector2(0.937f, 0.913f);
                __instance.CloseButtonEnabled = true;


                //SeamlessClient.TryShow("A2");
                //MyCommonTexts.ScreenCaptionPlayers

                //MyStringId ID = MyStringId.GetOrCompute("Test Caption");
                m_caption.SetValue(__instance, m_AddCaption.Invoke(__instance, new object[4] { MyCommonTexts.ScreenCaptionPlayers, null, new Vector2(0f, 0.003f), 0.8f }));


                float StartX = -0.435f;
                float StartY = -0.36f;

                MyGuiControlSeparatorList myGuiControlSeparatorList = new MyGuiControlSeparatorList();
                myGuiControlSeparatorList.AddHorizontal(new Vector2(StartX, StartY), .83f);




                Vector2 start = new Vector2(StartX, 0.358f);
                myGuiControlSeparatorList.AddHorizontal(start, 0.728f);
                myGuiControlSeparatorList.AddHorizontal(new Vector2(StartX, 0.05f), 0.17f);
                __instance.Controls.Add(myGuiControlSeparatorList);


                Vector2 Spacing = new Vector2(0f, 0.057f);
                Vector2 vector3 = new Vector2(StartX, StartY + 0.035f);

                //SeamlessClient.TryShow("B");

                MyGuiControlButton m_profileButton = new MyGuiControlButton(vector3, MyGuiControlButtonStyleEnum.Default, null, null, MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP, null, MyTexts.Get(MyCommonTexts.ScreenPlayers_Profile));
                m_profileButton.ButtonClicked += delegate (MyGuiControlButton obj) { m_profileButton_ButtonClicked.Invoke(__instance, new object[] { obj }); };
                __instance.Controls.Add(m_profileButton);
                vector3 += Spacing;
                m_ProfileButton.SetValue(__instance, m_profileButton);


                MyGuiControlButton m_promoteButton = new MyGuiControlButton(vector3, MyGuiControlButtonStyleEnum.Default, null, null, MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP, null, MyTexts.Get(MyCommonTexts.ScreenPlayers_Promote));
                m_promoteButton.ButtonClicked += delegate (MyGuiControlButton obj) { m_promoteButton_ButtonClicked.Invoke(__instance, new object[] { obj }); };
                __instance.Controls.Add(m_promoteButton);
                vector3 += Spacing;
                m_PromoteButton.SetValue(__instance, m_promoteButton);

                MyGuiControlButton m_demoteButton = new MyGuiControlButton(vector3, MyGuiControlButtonStyleEnum.Default, null, null, MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP, null, MyTexts.Get(MyCommonTexts.ScreenPlayers_Demote));
                m_demoteButton.ButtonClicked += delegate (MyGuiControlButton obj) { m_demoteButton_ButtonClicked.Invoke(__instance, new object[] { obj }); };
                __instance.Controls.Add(m_demoteButton);
                vector3 += Spacing;
                m_DemoteButton.SetValue(__instance, m_demoteButton);


                MyGuiControlButton m_kickButton = new MyGuiControlButton(vector3, MyGuiControlButtonStyleEnum.Default, null, null, MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP, null, MyTexts.Get(MyCommonTexts.ScreenPlayers_Kick));
                m_kickButton.ButtonClicked += delegate (MyGuiControlButton obj) { m_kickButton_ButtonClicked.Invoke(__instance, new object[] { obj }); };
                __instance.Controls.Add(m_kickButton);
                vector3 += Spacing;
                m_KickButton.SetValue(__instance, m_kickButton);

                MyGuiControlButton m_banButton = new MyGuiControlButton(vector3, MyGuiControlButtonStyleEnum.Default, null, null, MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP, null, MyTexts.Get(MyCommonTexts.ScreenPlayers_Ban));
                m_banButton.ButtonClicked += delegate (MyGuiControlButton obj) { m_banButton_ButtonClicked.Invoke(__instance, new object[] { obj }); };
                __instance.Controls.Add(m_banButton);
                vector3 += Spacing;
                m_BanButton.SetValue(__instance, m_banButton);


                MyGuiControlButton m_tradeButton = new MyGuiControlButton(vector3, MyGuiControlButtonStyleEnum.Default, null, null, MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP, null, MyTexts.Get(MySpaceTexts.PlayersScreen_TradeBtn));
                m_tradeButton.SetTooltip(MyTexts.GetString(MySpaceTexts.PlayersScreen_TradeBtn_TTP));
                m_tradeButton.ButtonClicked += delegate (MyGuiControlButton obj) { m_tradeButton_ButtonClicked.Invoke(__instance, new object[] { obj }); };
                __instance.Controls.Add(m_tradeButton);
                m_TradeButton.SetValue(__instance, m_tradeButton);



                //SeamlessClient.TryShow("C");

                Vector2 vector4 = vector3 + new Vector2(-0.002f, m_tradeButton.Size.Y + 0.03f);
                MyGuiControlCombobox m_lobbyTypeCombo = new MyGuiControlCombobox(vector4, null, null, null, 3);
                m_LobbyTypeCombo.SetValue(__instance, m_lobbyTypeCombo);

                Vector2 vector5 = vector4 + new Vector2(0f, 0.05f);
                vector5 += new Vector2(0f, 0.03f);
                int m_maxPlayers = (Sync.IsServer ? MyMultiplayerLobby.MAX_PLAYERS : 16);
                m_MaxPlayers.SetValue(__instance, m_maxPlayers);
                MyGuiControlSlider m_maxPlayersSlider = new MyGuiControlSlider(vector5, 2f, Math.Max(m_maxPlayers, 3), 0.177f, Sync.IsServer ? MySession.Static.MaxPlayers : MyMultiplayer.Static.MemberLimit, null, null, 1, 0.8f, 0f, "White", null, MyGuiControlSliderStyleEnum.Default, MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP, intValue: true);
                m_MaxPlayersSlider.SetValue(__instance, m_maxPlayersSlider);


                MyGuiControlButton m_inviteButton = new MyGuiControlButton(new Vector2(StartX, 0.25000026f), MyGuiControlButtonStyleEnum.Default, null, null, MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP, null, MyTexts.Get(MyCommonTexts.ScreenPlayers_Invite));
                m_inviteButton.ButtonClicked += delegate (MyGuiControlButton obj) { m_inviteButton_ButtonClicked.Invoke(__instance, new object[] { obj }); };
                __instance.Controls.Add(m_inviteButton);
                m_InviteButton.SetValue(__instance, m_inviteButton);

                Vector2 vector6 = new Vector2(-StartX - 0.034f, StartY + 0.033f);
                Vector2 size = new Vector2(0.66f, 1.2f);
                int num2 = 18;
                float num3 = 0f;


                //SeamlessClient.TryShow("D");
                MySessionComponentMatch component = MySession.Static.GetComponent<MySessionComponentMatch>();
                if (component.IsEnabled)
                {
                    Vector2 vector7 = __instance.GetPositionAbsolute() + vector6 + new Vector2(0f - size.X, 0f);
                    MyGuiControlLabel m_warfare_timeRemainting_label = new MyGuiControlLabel(vector6 - new Vector2(size.X, 0f));
                    m_warfare_timeRemainting_label.Text = MyTexts.GetString(MySpaceTexts.WarfareCounter_TimeRemaining).ToString() + ": ";
                    __instance.Controls.Add(m_warfare_timeRemainting_label);
                    m_Warfare_timeRemainting_label.SetValue(__instance, m_warfare_timeRemainting_label);


                    TimeSpan timeSpan = TimeSpan.FromMinutes(component.RemainingMinutes);
                    MyGuiControlLabel m_warfare_timeRemainting_time = new MyGuiControlLabel(vector6 - new Vector2(size.X, 0f) + new Vector2(m_warfare_timeRemainting_label.Size.X, 0f));
                    m_warfare_timeRemainting_time.Text = timeSpan.ToString((timeSpan.TotalHours >= 1.0) ? "hh\\:mm\\:ss" : "mm\\:ss");
                    __instance.Controls.Add(m_warfare_timeRemainting_time);
                    m_Warfare_timeRemainting_time.SetValue(__instance, m_warfare_timeRemainting_label);

                    float num4 = 0.09f;
                    float num5 = size.X / 3f - 2f * num3;
                    int num6 = 0;
                    MyFaction[] allFactions = MySession.Static.Factions.GetAllFactions();
                    foreach (MyFaction myFaction in allFactions)
                    {
                        if ((myFaction.Name.StartsWith("Red") || myFaction.Name.StartsWith("Green") || myFaction.Name.StartsWith("Blue")) && myFaction.Name.EndsWith("Faction"))
                        {
                            __instance.Controls.Add(new MyGuiScreenPlayersWarfareTeamScoreTable(vector7 + new Vector2((float)num6 * (num5 + num3), m_warfare_timeRemainting_label.Size.Y + num3), num5, num4, myFaction.Name, myFaction.FactionIcon.Value.String, MyTexts.GetString(MySpaceTexts.WarfareCounter_EscapePod), myFaction.FactionId, drawOwnBackground: false, drawBorders: true, myFaction.IsMember(MySession.Static.LocalHumanPlayer.Identity.IdentityId)));
                            num6++;
                        }
                    }
                    vector6.Y += m_warfare_timeRemainting_label.Size.Y + num4 + num3 * 2f;
                    num2 -= 3;
                }
                //SeamlessClient.TryShow("E");

                MyGuiControlTable m_playersTable = new MyGuiControlTable
                {
                    Position = vector6,
                    Size = size,
                    OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_TOP,
                    ColumnsCount = 7
                };

                m_PlayersTable.SetValue(__instance, m_playersTable);

                //SeamlessClient.TryShow("F");

                m_playersTable.GamepadHelpTextId = MySpaceTexts.PlayersScreen_Help_PlayersList;
                m_playersTable.VisibleRowsCount = num2;
                float PlayerName = 0.2f;
                float Rank = 0.1f;
                float Ping = 0.08f;
                float Muted = 0.1f;
                float SteamIcon = 0.04f;
                float Server = 0.20f;
                float FactionName = 1f - PlayerName - Rank - Muted - Ping - SteamIcon - Server;

                m_playersTable.SetCustomColumnWidths(new float[7]
                {
                    SteamIcon,
                    PlayerName,
                    FactionName,
                    Rank,
                    Ping,
                    Muted,
                    Server,
                });

                //SeamlessClient.TryShow("G");

                m_playersTable.SetColumnComparison(1, (MyGuiControlTable.Cell a, MyGuiControlTable.Cell b) => a.Text.CompareToIgnoreCase(b.Text));
                m_playersTable.SetColumnName(1, MyTexts.Get(MyCommonTexts.ScreenPlayers_PlayerName));
                m_playersTable.SetColumnComparison(2, (MyGuiControlTable.Cell a, MyGuiControlTable.Cell b) => a.Text.CompareToIgnoreCase(b.Text));
                m_playersTable.SetColumnName(2, MyTexts.Get(MyCommonTexts.ScreenPlayers_FactionName));
                m_playersTable.SetColumnName(5, new StringBuilder(MyTexts.GetString(MyCommonTexts.ScreenPlayers_Muted)));
                m_playersTable.SetColumnComparison(3, GameAdminCompare);
                m_playersTable.SetColumnName(3, MyTexts.Get(MyCommonTexts.ScreenPlayers_Rank));
                m_playersTable.SetColumnComparison(4, GamePingCompare);
                m_playersTable.SetColumnName(4, MyTexts.Get(MyCommonTexts.ScreenPlayers_Ping));


                StringBuilder colName = new StringBuilder("Server");
                m_playersTable.SetColumnName(6, colName);
                m_playersTable.SetColumnComparison(6, (MyGuiControlTable.Cell a, MyGuiControlTable.Cell b) => a.Text.CompareToIgnoreCase(b.Text));

                //SeamlessClient.TryShow("H");


                //m_PlayersTable_ItemSelected
                m_playersTable.ItemSelected += delegate (MyGuiControlTable i, MyGuiControlTable.EventArgs x) { m_PlayersTable_ItemSelected.Invoke(__instance, new object[] { i, x }); };
                m_playersTable.UpdateTableSortHelpText();
                __instance.Controls.Add(m_playersTable);


                string thisServerName = "thisServer";
                totalPlayerCount = 0;
                foreach(var server in AllServers)
                {

                    string servername = server.ServerName;
                    if (server.ServerID == currentServer)
                    {
                        thisServerName = servername;
                        _currentServerName = servername;
                        continue;
                    }

                    foreach (var player in server.Players)
                    {
                     
                        AddPlayer(__instance, player.SteamID, servername, player.PlayerName, player.IdentityID);
                        totalPlayerCount++;
                    }

                }



                currentPlayerCount = 0;
                foreach (MyPlayer onlinePlayer in Sync.Players.GetOnlinePlayers())
                {
                    if (onlinePlayer.Id.SerialId != 0)
                    {
                        continue;
                    }

                    currentPlayerCount++;
                    totalPlayerCount++;
                    AddPlayer(__instance, onlinePlayer.Id.SteamId, thisServerName);
                }

                //SeamlessClient.TryShow("I");

                ulong m_lastSelected = (ulong)m_LastSelected.GetValue(__instance);
                if (m_lastSelected != 0L)
                {
                    MyGuiControlTable.Row row2 = m_playersTable.Find((MyGuiControlTable.Row r) => (ulong)r.UserData == m_lastSelected);
                    if (row2 != null)
                    {
                        m_playersTable.SelectedRow = row2;
                    }
                }

                m_UpdateButtonsEnabledState.Invoke(__instance, null);
                //UpdateButtonsEnabledState();

                m_UpdateCaption.Invoke(__instance, null);




                Vector2 minSizeGui = MyGuiControlButton.GetVisualStyle(MyGuiControlButtonStyleEnum.Default).NormalTexture.MinSizeGui;
                MyGuiControlLabel myGuiControlLabel = new MyGuiControlLabel(new Vector2(start.X, start.Y + minSizeGui.Y / 2f));
                myGuiControlLabel.Name = MyGuiScreenBase.GAMEPAD_HELP_LABEL_NAME;
                __instance.Controls.Add(myGuiControlLabel);
                __instance.GamepadHelpTextId = MySpaceTexts.PlayersScreen_Help_Screen;
                __instance.FocusedControl = m_playersTable;

                //SeamlessClient.TryShow("J");

            }
            catch (Exception ex)
            {
                SeamlessClient.TryShow(ex.ToString());
            }

            return false;
        }

        private static bool AddPlayer(MyGuiScreenPlayers __instance, ulong userId, string server, string playername = null, long playerId = 0)
        {
            MyGuiControlTable table = (MyGuiControlTable)m_playersTable.GetValue(__instance);
            Dictionary<ulong, short> pings = (Dictionary<ulong, short>)m_pings.GetValue(__instance);

            if(playername == null)
                 playername = MyMultiplayer.Static.GetMemberName(userId);

            if (string.IsNullOrEmpty(playername))
            {
                return false;
            }


            MyGuiControlTable.Row row = new MyGuiControlTable.Row(userId);
            string memberServiceName = MyMultiplayer.Static.GetMemberServiceName(userId);
            StringBuilder text = new StringBuilder();



            MyGuiHighlightTexture? icon = new MyGuiHighlightTexture
            {
                Normal = "Textures\\GUI\\Icons\\Services\\" + memberServiceName + ".png",
                Highlight = "Textures\\GUI\\Icons\\Services\\" + memberServiceName + ".png",
                SizePx = new Vector2(24f, 24f)
            };
            row.AddCell(new MyGuiControlTable.Cell(text, null, memberServiceName, Color.White, icon, MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER));
            row.AddCell(new MyGuiControlTable.Cell(new StringBuilder(playername), playername));


            if(playerId == 0)
                playerId = Sync.Players.TryGetIdentityId(userId);

        

            MyFaction playerFaction = MySession.Static.Factions.GetPlayerFaction(playerId);
            string text2 = "";
            StringBuilder stringBuilder = new StringBuilder();
            if (playerFaction != null)
            {
                text2 += playerFaction.Name;
                text2 = text2 + " | " + playername;
                foreach (KeyValuePair<long, MyFactionMember> member in playerFaction.Members)
                {
                    if ((member.Value.IsLeader || member.Value.IsFounder) && MySession.Static.Players.TryGetPlayerId(member.Value.PlayerId, out var result) && MySession.Static.Players.TryGetPlayerById(result, out var player))
                    {
                        text2 = text2 + " | " + player.DisplayName;
                        break;
                    }
                }
                stringBuilder.Append(MyStatControlText.SubstituteTexts(playerFaction.Name));
                if (playerFaction.IsLeader(playerId))
                {
                    stringBuilder.Append(" (").Append((object)MyTexts.Get(MyCommonTexts.Leader)).Append(")");
                }
                if (!string.IsNullOrEmpty(playerFaction.Tag))
                {
                    stringBuilder.Insert(0, "[" + playerFaction.Tag + "] ");
                }
            }
            row.AddCell(new MyGuiControlTable.Cell(stringBuilder, null, text2));
            StringBuilder stringBuilder2 = new StringBuilder();
            MyPromoteLevel userPromoteLevel = MySession.Static.GetUserPromoteLevel(userId);
            for (int i = 0; i < (int)userPromoteLevel; i++)
            {
                stringBuilder2.Append("*");
            }
            row.AddCell(new MyGuiControlTable.Cell(stringBuilder2));
            if (pings.ContainsKey(userId))
            {
                row.AddCell(new MyGuiControlTable.Cell(new StringBuilder(pings[userId].ToString())));
            }
            else
            {
                row.AddCell(new MyGuiControlTable.Cell(new StringBuilder("----")));
            }
            MyGuiControlTable.Cell cell = new MyGuiControlTable.Cell(new StringBuilder(""));
            row.AddCell(cell);
            if (userId != Sync.MyId)
            {
                MyGuiControlButton myGuiControlButton = new MyGuiControlButton();
                myGuiControlButton.CustomStyle = m_buttonSizeStyleUnMuted;
                myGuiControlButton.Size = new Vector2(0.03f, 0.04f);
                myGuiControlButton.CueEnum = GuiSounds.None;


                Action<MyGuiControlButton> btnClicked = delegate (MyGuiControlButton b)
               {
                   m_OnToggleMutePressed.Invoke(__instance, new object[] { b });
               };



                myGuiControlButton.ButtonClicked += btnClicked;
                myGuiControlButton.UserData = userId;
                cell.Control = myGuiControlButton;
                table.Controls.Add(myGuiControlButton);
                m_RefreshMuteIcons.Invoke(__instance, null);
                //RefreshMuteIcons();
            }
            table.Add(row);
            m_UpdateCaption.Invoke(__instance, null);

            row.AddCell(new MyGuiControlTable.Cell(new StringBuilder(server), "Server Name"));

            return false;
        }

        private static bool UpdateCaption(MyGuiScreenPlayers __instance)
        {
            if (MyMultiplayer.Static != null && MyMultiplayer.Static.IsLobby)
            {
                return true;
            }

            MyGuiControlLabel mM_caption = (MyGuiControlLabel)m_caption.GetValue(__instance);
            MyGuiControlTable mm_playersTable = (MyGuiControlTable)m_playersTable.GetValue(__instance);
          


            //string s = $"{MyTexts.Get(MyCommonTexts.ScreenCaptionServerName).ToString()} - SectorPlayers: ({ mm_playersTable.RowsCount} / {MySession.Static.MaxPlayers}) TotalPlayers: ( {5} / 200 )";

            mM_caption.Text = string.Concat("Server: ", _currentServerName, "  -  ", "Innstance Players", " (", currentPlayerCount, " / ", MySession.Static.MaxPlayers, ")    TotalPlayers: ( ", totalPlayerCount, " )");


            return false;
        }

        private static int GamePingCompare(MyGuiControlTable.Cell a, MyGuiControlTable.Cell b)
        {
            if (!int.TryParse(a.Text.ToString(), out var result))
            {
                result = -1;
            }
            if (!int.TryParse(b.Text.ToString(), out var result2))
            {
                result2 = -1;
            }
            return result.CompareTo(result2);
        }
        private static int GameAdminCompare(MyGuiControlTable.Cell a, MyGuiControlTable.Cell b)
        {
            ulong steamId = (ulong)a.Row.UserData;
            ulong steamId2 = (ulong)b.Row.UserData;
            int userPromoteLevel = (int)MySession.Static.GetUserPromoteLevel(steamId);
            int userPromoteLevel2 = (int)MySession.Static.GetUserPromoteLevel(steamId2);
            return userPromoteLevel.CompareTo(userPromoteLevel2);
        }


        private static readonly MyGuiControlButton.StyleDefinition m_buttonSizeStyleUnMuted = new MyGuiControlButton.StyleDefinition
        {
            NormalFont = "White",
            HighlightFont = "White",
            NormalTexture = MyGuiConstants.TEXTURE_HUD_VOICE_CHAT,
            HighlightTexture = MyGuiConstants.TEXTURE_HUD_VOICE_CHAT
        };

        private static MethodInfo GetPatchMethod(string v)
        {
            return typeof(OnlinePlayers).GetMethod(v, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        }

    }
}
