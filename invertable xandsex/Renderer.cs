using System.Collections.Concurrent;
using System.Numerics;
using System.Runtime.InteropServices;
using ClickableTransparentOverlay;
using ImGuiNET;
using System.Drawing;


namespace invertable_xandsex
{

    public class Renderer : Overlay
    {
        public Vector2 screenSize = new Vector2(1920, 1080);
        public System.Drawing.Size windowSize = new System.Drawing.Size(1920, 1080);
        public ConcurrentQueue<Entity> entities = new ConcurrentQueue<Entity>();
        private Entity localPlayer = new Entity();
        private readonly object entityLock = new object();

        public bool enableESP = true;
        public bool enableBHOP = false;
        public bool enableTraces = false;
        public bool enableBoxes = false;
        public bool enableHealthBox = false;
        public bool enableTriggerbot = false;
        public bool espTeamcheck = true;
        public bool removeESP4team = false;
        public bool showPlayerNameESP = false;
        public bool showPlayerWeaponName = false;
        public bool enableAimbot = false;
        public bool visibleOnlyESP = false;
        public bool nocheatsonSpectate = false;
        public int triggerbotDelay = 100;

        private Vector4 enemyBoxColor = new Vector4(1, 0, 0, 1);
        private Vector4 teamBoxColor = new Vector4(0, 1, 0, 1);
        private Vector4 boxFillColor = new Vector4(0, 1, 0, 1);
        private Vector4 hiddenColor = new Vector4(0, 1, 1, 1);

        ImDrawListPtr drawList;

        public void ChangeOverlaySize(Size newSize)
        {
            Size = newSize;
        }


        protected override void Render()
        {
            ChangeOverlaySize(windowSize);

            ImGui.SetNextWindowSize(new System.Numerics.Vector2(600, 400));
            ImGui.Begin("Invertable :33", ImGuiWindowFlags.NoResize);
            if (ImGui.BeginTabBar("SettingsTabBar", ImGuiTabBarFlags.FittingPolicyResizeDown))
            {
                if (ImGui.BeginTabItem("ESP"))
                {
                    ImGui.Checkbox("Enable ESP", ref enableESP);
                    ImGui.Separator();
                    ImGui.TextColored(new System.Numerics.Vector4(1, 1, 0, 1), "ESP Options:");
                    ImGui.Indent();
                    ImGui.Checkbox("Tracers", ref enableTraces);
                    ImGui.Checkbox("Boxes", ref enableBoxes);
                    ImGui.Checkbox("Health Bar", ref enableHealthBox);
                    ImGui.Checkbox("Team Check", ref espTeamcheck);
                    ImGui.Checkbox("No ESP for Teammates", ref removeESP4team);
                    ImGui.Checkbox("Player Names", ref showPlayerNameESP);
                    ImGui.Checkbox("Player Weapons", ref showPlayerWeaponName);
                    ImGui.Checkbox("Visible Only", ref visibleOnlyESP);
                    ImGui.Checkbox("No ESP on Spectate", ref nocheatsonSpectate);
                    ImGui.Unindent();

                    ImGui.Separator();
                    ImGui.TextColored(new System.Numerics.Vector4(1, 0, 1, 1), "Color Settings:");
                    ImGui.Indent();
                    ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new System.Numerics.Vector2(4, 4));
                    if (ImGui.CollapsingHeader("Team Color"))
                        ImGui.ColorPicker4("##teamcolor", ref teamBoxColor);
                    if (ImGui.CollapsingHeader("Enemy Color"))
                        ImGui.ColorPicker4("##enemycolor", ref enemyBoxColor);
                    if (ImGui.CollapsingHeader("Box Fill Color"))
                        ImGui.ColorPicker4("##boxfill", ref boxFillColor);
                    if (ImGui.CollapsingHeader("Not Visible Box Color"))
                        ImGui.ColorPicker4("##notvisibleboxcolor", ref hiddenColor);
                    ImGui.PopStyleVar();
                    ImGui.Unindent();

                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("Triggerbot"))
                {
                    ImGui.Checkbox("Enable Triggerbot", ref enableTriggerbot);
                    ImGui.SliderInt("Triggerbot Delay", ref triggerbotDelay, 1, 1000);
                    ImGui.EndTabItem();
                }
                ImGui.EndTabBar();
            }


            DrawOverlay(screenSize);
            drawList = ImGui.GetWindowDrawList();

            if (enableESP)
            {
                foreach (var entity in entities)
                {
                    if (EntityOnScreen(entity))
                    {
                        if (enableTraces)
                        {
                            DrawLine(entity);
                        }
                        if (enableHealthBox)
                            DrawHealth(entity);

                        if (enableBoxes)
                        {
                            DrawBox(entity);
                        }

                        if (showPlayerNameESP)
                        {
                            DrawPlayerName(entity);
                        }
                        if (showPlayerWeaponName)
                        {
                            DrawPlayerWeapon(entity);
                        }
                    }
                }
            }

            ImGui.End();
        } 
        bool EntityOnScreen(Entity entity)
        {
            if (entity.position2D.X > 0 && entity.position2D.X < screenSize.X && entity.position2D.Y > 0 && entity.position2D.Y < screenSize.Y);
            {
                return true;
            }
        }
        private void DrawPlayerName(Entity entity)
        {
            if (showPlayerNameESP)
            {
                string playerName = entity.entityName;
                int i = playerName.IndexOf('?');
                playerName = i != -1 ? playerName.Substring(0, i) : playerName;

                Vector2 textSize = ImGui.CalcTextSize(playerName);
                Vector2 textPosition = new Vector2((entity.viewPosition2D.X + entity.position2D.X - textSize.X) / 2, entity.position2D.Y + 5);
                Vector4 textColor = new Vector4(1, 1, 1, 1);

                drawList.AddText(textPosition, ImGui.ColorConvertFloat4ToU32(textColor), playerName);
            }
        }

        private void DrawPlayerWeapon(Entity entity)
        {
            if (showPlayerWeaponName)
            {
                Vector2 textSize = ImGui.CalcTextSize(entity.entityWeapon);
                Vector2 textPosition = new Vector2((entity.viewPosition2D.X + entity.position2D.X - textSize.X) / 2, entity.viewPosition2D.Y - 20);
                Vector4 textColor = new Vector4(1, 1, 1, 1);

                drawList.AddText(textPosition, ImGui.ColorConvertFloat4ToU32(textColor), entity.entityWeapon);
            }
        }

        private void DrawBox(Entity entity)
        {
            float h = entity.position2D.Y - entity.viewPosition2D.Y;

            if (enableBoxes)
            {
                Vector2 t = new Vector2(entity.viewPosition2D.X - h / 3, entity.viewPosition2D.Y);
                Vector2 b = new Vector2(entity.position2D.X + h / 3, entity.position2D.Y);

                Vector4 c = espTeamcheck ? (localPlayer.team == entity.team ? teamBoxColor : enemyBoxColor) : enemyBoxColor;
                if (visibleOnlyESP)
                    c = entity.spotted ? c : hiddenColor;

                Vector4 f = boxFillColor;

                drawList.AddRectFilledMultiColor(t, b, ImGui.ColorConvertFloat4ToU32(f), ImGui.ColorConvertFloat4ToU32(f), ImGui.ColorConvertFloat4ToU32(c), ImGui.ColorConvertFloat4ToU32(c));
                drawList.AddRect(t, b, ImGui.ColorConvertFloat4ToU32(c));
            }
        }

        private void DrawHealth(Entity entity)
        {
            float h = entity.position2D.Y - entity.viewPosition2D.Y;
            float l = entity.viewPosition2D.X - h / 3;
            float r = entity.position2D.X + h / 3;
            float w = 0.05f * (r - l);

            float hp = entity.health;
            Vector2 t = new Vector2(l - w - 2, entity.position2D.Y - h * (hp / 100f));
            Vector2 b = new Vector2(l - 2, entity.position2D.Y);
            Vector4 c;
            if (hp <= 33)
                c = new Vector4(1, 0, 0, 1);
            else if (hp <= 66)
                c = new Vector4(1, 1, 0, 1);
            else
                c = new Vector4(0, 1, 0, 1);
            Vector2 otl = new Vector2(t.X - 1, t.Y - 1);
            Vector2 obr = new Vector2(b.X + 1, b.Y + 1);

            drawList.AddRectFilledMultiColor(otl, obr, ImGui.GetColorU32(ImGuiCol.Border), ImGui.GetColorU32(ImGuiCol.Border), ImGui.ColorConvertFloat4ToU32(c), ImGui.ColorConvertFloat4ToU32(c));
            drawList.AddRect(otl, obr, ImGui.GetColorU32(ImGuiCol.Border), 0, ImDrawFlags.RoundCornersTopLeft | ImDrawFlags.RoundCornersBottomLeft);
        }

        private void DrawLine(Entity entity)
        {
            Vector4 c = espTeamcheck ? (localPlayer.team == entity.team ? teamBoxColor : enemyBoxColor) : enemyBoxColor;
            drawList.AddLine(new Vector2(screenSize.X / 2, screenSize.Y), entity.position2D, ImGui.ColorConvertFloat4ToU32(c));
        }

        public void UpdateEntities(IEnumerable<Entity> newEntities)
        {
            entities = new ConcurrentQueue<Entity>(newEntities);
        }
        public void UpdateLocalPlayer(Entity newEntity)
        {
            lock (entityLock)
            {
                localPlayer = newEntity;
            }
        }
        public Entity GetLocalPlayer()
        {
            lock(entityLock)
            {
                return localPlayer;
            }
        }
        void DrawOverlay(Vector2 screenSize)
        {
            ImGui.SetNextWindowSize(screenSize);
            ImGui.Begin("sigma", ImGuiWindowFlags.NoDecoration |
                                     ImGuiWindowFlags.NoBackground |
                                     ImGuiWindowFlags.NoBringToFrontOnFocus |
                                     ImGuiWindowFlags.NoMove |
                                     ImGuiWindowFlags.NoInputs |
                                     ImGuiWindowFlags.NoCollapse |
                                     ImGuiWindowFlags.NoScrollbar |
                                     ImGuiWindowFlags.NoScrollWithMouse);
        }

        [DllImport("user32.dll")]
        static extern short GetAsyncKeyState(int vKey);
    }
}
