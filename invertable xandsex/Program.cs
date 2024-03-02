using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using ImGuiNET;
using invertable_xandsex;
using Swed64;


public static class WeaponIndex
{
    public static Dictionary<string, int> IndexToWeapon = new Dictionary<string, int>()
    {
        { "T knife", 59 },
        { "CT knife", 42 },
        { "deagle", 1 },
        { "elite", 2 },
        { "fiveseven", 3 },
        { "glock", 4 },
        { "revolver", 64 },
        { "p2000", 32 },
        { "p250", 36 },
        { "usp-s", 61 },
        { "tec9", 30 },
        { "cz75a", 63 },
        { "mac10", 17 },
        { "ump45", 24 },
        { "bizon", 26 },
        { "mp7", 33 },
        { "mp9", 34 },
        { "p90", 19 },
        { "galil", 13 },
        { "famas", 10 },
        { "m4a1_silencer", 60 },
        { "m4a4", 16 },
        { "aug", 8 },
        { "sg556", 39 },
        { "ak47", 7 },
        { "g3sg1", 11 },
        { "scar20", 38 },
        { "awp", 9 },
        { "ssg08", 40 },
        { "xm1014", 25 },
        { "sawedoff", 29 },
        { "mag7", 27 },
        { "nova", 35 },
        { "negev", 28 },
        { "m249", 14 },
        { "zeus", 31 },
        { "flashbang", 43 },
        { "hegrenade", 44 },
        { "smokegrenade", 45 },
        { "molotov", 46 },
        { "decoy", 47 },
        { "incgrenade", 48 },
        { "c4", 49 }
    };
}

class Program
{
    static void Main(string[] args)
    {
        Swed niggers = new Swed("cs2");
        IntPtr client = niggers.GetModuleBase("client.dll");

        Renderer renderer = new Renderer();
        Thread renderThread = new Thread(() => renderer.Start());
        renderThread.Start();

        Vector2 screenSize = renderer.screenSize;

        Thread espThread = new Thread(() => ESPFeature(client, niggers, renderer, screenSize));
        Thread triggerBotThread = new Thread(() => TriggerbotFeature(client, niggers, renderer));

        espThread.Start();
        triggerBotThread.Start();
    }

    static void ESPFeature(IntPtr client, Swed niggers, Renderer renderer, Vector2 screenSize)
    {
        List<Entity> entities = new List<Entity>();
        Entity localPlayer = new Entity();
        int numThreads = Environment.ProcessorCount;

        while (true)
        {
            if (renderer.enableESP)
            {
                IntPtr entityList = niggers.ReadPointer(client, client_dll.dwEntityList);
                IntPtr listEntry = niggers.ReadPointer(entityList, 0x10);
                IntPtr localPlayerPawn = niggers.ReadPointer(client, client_dll.dwLocalPlayerPawn);
                if (listEntry == IntPtr.Zero) continue;
                if (localPlayerPawn == IntPtr.Zero) continue;
                entities.Clear();
                localPlayer.team = niggers.ReadInt(localPlayerPawn, C_BaseEntity.m_iTeamNum);

                Thread[] threads = new Thread[numThreads];
                List<Entity>[] entityChunks = new List<Entity>[numThreads];

                for (int i = 0; i < numThreads; i++)
                {
                    entityChunks[i] = new List<Entity>();
                }

                for (int i = 0; i < 64; i++)
                {
                    IntPtr currentController = niggers.ReadPointer(listEntry, i * 0x78);

                    if (currentController == IntPtr.Zero) continue;

                    int pawnHandle = niggers.ReadInt(currentController, CCSPlayerController.m_hPlayerPawn);
                    if (pawnHandle == 0) continue;

                    IntPtr listEntry2 = niggers.ReadPointer(entityList, 0x8 * ((pawnHandle & 0x7FFF) >> 9) + 0x10);
                    if (listEntry2 == IntPtr.Zero) continue;

                    IntPtr currentPawn = niggers.ReadPointer(listEntry2, 0x78 * (pawnHandle & 0x1FF));
                    if (currentPawn == IntPtr.Zero) continue;

                    int lifeState = niggers.ReadInt(currentPawn, C_BaseEntity.m_lifeState);
                    if (lifeState != 256) continue;

                    int entityTeam = niggers.ReadInt(currentPawn, C_BaseEntity.m_iTeamNum);
                    if (entityTeam == localPlayer.team && renderer.removeESP4team) continue;


                    float[] viewMatrix = niggers.ReadMatrix(client + client_dll.dwViewMatrix);
                    Entity entity = new Entity();
                    entity.team = niggers.ReadInt(currentPawn, C_BaseEntity.m_iTeamNum);
                    entity.spotted = niggers.ReadBool(currentPawn, C_CSPlayerPawnBase.m_entitySpottedState + EntitySpottedState_t.m_bSpotted);
                    entity.health = niggers.ReadInt(currentPawn, C_BaseEntity.m_iHealth);
                    entity.position = niggers.ReadVec(currentPawn, C_BasePlayerPawn.m_vOldOrigin);
                    entity.viewOffset = niggers.ReadVec(currentPawn, C_BaseModelEntity.m_vecViewOffset);
                    entity.position2D = Calculate.WorldToScreen(viewMatrix, entity.position, screenSize);
                    entity.viewPosition2D = Calculate.WorldToScreen(viewMatrix, Vector3.Add(entity.position, entity.viewOffset), screenSize);
                    string player_name = niggers.ReadString(currentController, CBasePlayerController.m_iszPlayerName, 128);
                    entity.entityName = Regex.Replace(player_name, @"[^a-zA-Zа-яА-Я]", "");

                    IntPtr currentWeaponID = niggers.ReadPointer(currentPawn, C_CSPlayerPawnBase.m_pClippingWeapon);
                    ushort itemDefinitionIndex = niggers.ReadUShort((currentWeaponID + C_EconEntity.m_AttributeManager + C_AttributeContainer.m_Item + C_EconItemView.m_iItemDefinitionIndex));
                    entity.entityWeapon = WeaponIndex.IndexToWeapon.FirstOrDefault(x => x.Value == itemDefinitionIndex).Key ?? "";

                    if (currentPawn != localPlayerPawn)
                    {
                        entityChunks[i % numThreads].Add(entity);
                    }

                }
                for (int i = 0; i < numThreads; i++)
                {
                    int index = i;
                    threads[i] = new Thread(() => ProcessChunk(entityChunks[index], entities, localPlayer, renderer));
                    threads[i].Start();
                }

                foreach (var thread in threads)
                {
                    thread.Join();
                }


                renderer.UpdateLocalPlayer(localPlayer);
                renderer.UpdateEntities(entities);
                Thread.Sleep(5);
            }
            else
            {
                Thread.Sleep(20);
            }
        }
    }

    static void ProcessChunk(List<Entity> chunk, List<Entity> entities, Entity localPlayer, Renderer renderer)
    {
        foreach (var entity in chunk)
        {
            entities.Add(entity);
        }
    }


    static void TriggerbotFeature(IntPtr client, Swed niggers, Renderer renderer)
    {
        while (true)
        {
            if (renderer.enableTriggerbot)
            {
                IntPtr entityList = niggers.ReadPointer(client, client_dll.dwEntityList);
                IntPtr localPlayerPawn = niggers.ReadPointer(client, client_dll.dwLocalPlayerPawn);

                if (localPlayerPawn != IntPtr.Zero)
                {
                    int entIndex = niggers.ReadInt(localPlayerPawn, C_CSPlayerPawnBase.m_iIDEntIndex);

                    if (entIndex != -1)
                    {
                        IntPtr listEntry = niggers.ReadPointer(entityList, 0x8 * ((entIndex & 0x7FFF) >> 9) + 0x10);
                        IntPtr currentPawn = niggers.ReadPointer(listEntry, 0x78 * (entIndex & 0x1FF));

                        int entityTeam = niggers.ReadInt(currentPawn, C_BaseEntity.m_iTeamNum);
                        int localPlayerTeam = niggers.ReadInt(localPlayerPawn, C_BaseEntity.m_iTeamNum);

                        if (localPlayerTeam != entityTeam)
                        {
                            if ((GetAsyncKeyState(0x06) & 0x8000) != 0)
                            {
                                Thread.Sleep(renderer.triggerbotDelay);
                                MouseEvent(0x0002, 0, 0, 0, UIntPtr.Zero);
                                Thread.Sleep(10);
                                MouseEvent(0x0004, 0, 0, 0, UIntPtr.Zero);
                            }
                        }
                    }
                }

                Thread.Sleep(2);
            }
            Thread.Sleep(5);
        }
    }

    static void MouseEvent(uint dwFlags, uint dx, uint dy, uint dwData, UIntPtr dwExtraInfo)
    {
        mouse_event(dwFlags, dx, dy, dwData, dwExtraInfo);
    }

    [DllImport("user32.dll")]
    static extern short GetAsyncKeyState(int vKey);

    [DllImport("user32.dll", SetLastError = true)]
    static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, UIntPtr dwExtraInfo);
}