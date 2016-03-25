/*
    Intersect Game Engine (Server)
    Copyright (C) 2015  JC Snider, Joe Bridges
    
    Website: http://ascensiongamedev.com
    Contact Email: admin@ascensiongamedev.com 

    This program is free software; you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation; either version 2 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License along
    with this program; if not, write to the Free Software Foundation, Inc.,
    51 Franklin Street, Fifth Floor, Boston, MA 02110-1301 USA.
*/
using System;
using System.Collections.Generic;
using System.Threading;
using Intersect_Server.Classes.Game_Objects;

namespace Intersect_Server.Classes
{
	public static class Globals
	{

        //Console Variables
        public static List<string> GeneralLogs = new List<string>();
        public static long CPS = 0;
        public static Boolean CPSLock = true;
        public static bool ServerStarted = true;

        //Options
        public static int ServerPort = 4500;
        public static string MySqlHost = "";
        public static int MySqlPort = 3306;
        public static string MySqlUser = "";
        public static string MySqlPass = "";
        public static string MySqldb = "";
	    public static int GameBorderStyle = 0; //0 For Smart Borders, 1 for Non-Seamless, 2 for black borders

        //Map Options
        public static int MapWidth = 32;
        public static int MapHeight = 26;

        //Game Maps
		public static MapStruct[] GameMaps;
		public static int MapCount;

        //Game Items
        public static ItemStruct[] GameItems;

        //Game Shops
        public static ShopStruct[] GameShops;

        //Game Spells
        public static SpellStruct[] GameSpells;

        //Game Npcs
        public static NpcStruct[] GameNpcs;

        //Game Resources
        public static ResourceStruct[] GameResources;

        //Game Animations
        public static AnimationStruct[] GameAnimations;

        //Game Classes
        public static ClassStruct[] GameClasses;

        //Game Quests
        public static QuestStruct[] GameQuests;

        //Game Projectiles
        public static ProjectileStruct[] GameProjectiles;

        //Common Events
	    public static EventStruct[] CommonEvents;

        //Server Switches and Variables
	    public static string[] ServerSwitches;
	    public static bool[] ServerSwitchValues;
	    public static string[] ServerVariables;
	    public static int[] ServerVariableValues;

        //Player Switches and Variables
        public static string[] PlayerSwitches;
        public static string[] PlayerVariables;

        public static List<Client> Clients = new List<Client>();
		public static List<Thread> ClientThread = new List<Thread>();
		public static List<Entity> Entities = new List<Entity>();
		public static string[] Tilesets;

        //Game helping stuff
        public static Random Rand = new Random();

        public static int FindOpenEntity()
        {
            for (var i = 0; i < Entities.Count; i++)
            {
                if (Entities[i] == null)
                {
                    return i;
                }
                else if (i == Entities.Count - 1)
                {
                    Entities.Add(null);
                    return Entities.Count - 1;
                }
            }
            Entities.Add(null);
            return Entities.Count - 1;
        }

        public static void AddEntity(Entity newEntity)
        {
            var slot = FindOpenEntity();
            Entities[slot] = newEntity;
        }
	}
}

