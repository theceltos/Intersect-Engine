﻿/*
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
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Collections.Generic;
using System.Runtime.Remoting.Messaging;
using System.Security.Cryptography.X509Certificates;
using System.Xml;
using System.Security.Cryptography;
using System.Text;
using Intersect_Server.Classes.Game_Objects;
using Intersect_Server.Classes.Maps;
using MySql.Data.MySqlClient;
using Intersect_Server.Classes.Networking;

namespace Intersect_Server.Classes
{
    public static class Database
    {
        public static bool MySQLConnected;
        public static object MapGridLock = new Object();
        public static List<MapGrid> MapGrids = new List<MapGrid>();
        public static string ConnectionString = "";
        public static MapList MapStructure = new MapList();

        public static List<string> Emails = new List<string>();
        public static List<string> Accounts = new List<string>();
        public static List<string> Characters = new List<string>();

        private enum MySqlFields
        {
            m_string = 0,
            m_int
        }

        //Check Directories
        public static void CheckDirectories()
        {
            if (!Directory.Exists("resources")) { Directory.CreateDirectory("resources"); }
            if (!Directory.Exists("resources/accounts")) { Directory.CreateDirectory("resources/accounts"); }
        }

        //Options File
        public static bool LoadOptions()
        {

            if (!File.Exists("resources\\config.xml"))
            {
                var settings = new XmlWriterSettings { Indent = true };
                var writer = XmlWriter.Create("resources\\config.xml", settings);
                writer.WriteStartDocument();
                writer.WriteComment("Config.xml generated automatically by Intersect Server.");
                writer.WriteStartElement("Config");
                writer.WriteElementString("ServerPort", "4500");
                writer.WriteElementString("DBHost", "");
                writer.WriteElementString("DBPort", "");
                writer.WriteElementString("DBUser", "");
                writer.WriteElementString("DBPass", "");
                writer.WriteElementString("DBName", "");
                writer.WriteComment("MapBorder Override. 0 for seamless with scrolling that stops on world edges. 1 for non-seamless, and 2 for seamless where the camera knows no boundaries. (Black borders where the world ends)");
                writer.WriteElementString("BorderStyle", "0");
                writer.WriteComment("Do NOT touch these values will resize the maps in the engine. If you have existing maps and change these values you MUST delete them or else the engine will crash on launch.");
                writer.WriteElementString("MapWidth", Globals.MapWidth.ToString());
                writer.WriteElementString("MapHeight", Globals.MapHeight.ToString());
                writer.WriteEndElement();
                writer.WriteEndDocument();
                writer.Flush();
                writer.Close();
            }
            else
            {
                var options = new XmlDocument();
                try
                {
                    options.Load("resources\\config.xml");
                    var selectSingleNode = options.SelectSingleNode("//Config/ServerPort");
                    if (selectSingleNode != null && selectSingleNode.InnerText != "")
                        Globals.ServerPort = Int32.Parse(selectSingleNode.InnerText);
                    selectSingleNode = options.SelectSingleNode("//Config/DBHost");
                    if (selectSingleNode != null && selectSingleNode.InnerText != "")
                        Globals.MySqlHost = selectSingleNode.InnerText;
                    selectSingleNode = options.SelectSingleNode("//Config/DBPort");
                    if (selectSingleNode != null && selectSingleNode.InnerText != "")
                        Globals.MySqlPort = Int32.Parse(selectSingleNode.InnerText);
                    selectSingleNode = options.SelectSingleNode("//Config/DBUser");
                    if (selectSingleNode != null && selectSingleNode.InnerText != "")
                        Globals.MySqlUser = selectSingleNode.InnerText;
                    selectSingleNode = options.SelectSingleNode("//Config/DBPass");
                    if (selectSingleNode != null && selectSingleNode.InnerText != "")
                        Globals.MySqlPass = selectSingleNode.InnerText;
                    selectSingleNode = options.SelectSingleNode("//Config/DBName");
                    if (selectSingleNode != null && selectSingleNode.InnerText != "")
                        Globals.MySqldb = selectSingleNode.InnerText;
                    selectSingleNode = options.SelectSingleNode("//Config/BorderStyle");
                    if (selectSingleNode != null && selectSingleNode.InnerText != "")
                        Globals.GameBorderStyle = Int32.Parse(selectSingleNode.InnerText);
                    selectSingleNode = options.SelectSingleNode("//Config/MapWidth");
                    if (selectSingleNode != null && selectSingleNode.InnerText != "")
                        Globals.MapWidth = Int32.Parse(selectSingleNode.InnerText);
                    selectSingleNode = options.SelectSingleNode("//Config/MapHeight");
                    if (selectSingleNode != null && selectSingleNode.InnerText != "")
                        Globals.MapHeight = Int32.Parse(selectSingleNode.InnerText);
                }
                catch (Exception)
                {
                    return false;
                }
            }
            return true;
        }

        //Players General
        public static void LoadPlayerDatabase()
        {
            //This function determines if players should be saved and loaded via XML or MySQL
            if (Globals.MySqlHost != "")
            {
                //Try MySQL
                if (InitMySql())
                {
                    MySQLConnected = true;
                    Console.WriteLine("Connected to the MySQL database successfully.");
                    return;
                }
                else
                {
                    Console.WriteLine("Failed to connect to the MySQL database.");
                }
            }
            Console.WriteLine("Using local file system for account database.");
            LoadAccounts();
        }
        public static bool AccountExists(string accountname)
        {
            if (MySQLConnected)
            {
                return AccountExistsSQL(accountname);
            }
            else
            {
                return AccountExistsXML(accountname);
            }
        }
        public static bool EmailInUse(string email)
        {
            if (MySQLConnected)
            {
                return EmailInUseSQL(email);
            }
            else
            {
                return EmailInUseXML(email);
            }
        }
        public static bool CharacterNameInUse(string name)
        {
            if (MySQLConnected)
            {
                return CharacterNameInUseSQL(name);
            }
            else
            {
                return CharacterNameInUseXML(name);
            }
        }
        public static int GetRegisteredPlayers()
        {
            if (MySQLConnected)
            {
                return GetRegisteredPlayersSQL();
            }
            else
            {
                return GetRegisteredPlayersXML();
            }
        }
        public static bool CheckPassword(string username, string password)
        {
            if (MySQLConnected)
            {
                return CheckPasswordSQL(username,password);
            }
            else
            {
                return CheckPasswordXML(username,password);
            }
        }
        public static void CreateAccount(Player en, string username, string password, string email)
        {
            if (MySQLConnected)
            {
                CreateAccountSQL(en,username, password,email);
            }
            else
            {
                CreateAccountXML(en, username, password, email);
            }
        }
        public static bool LoadPlayer(Client client)
        {
            if (MySQLConnected)
            {
                return LoadPlayerSQL(client);
            }
            else
            {
                return LoadPlayerXML(client);
            }
        }
        public static void SavePlayer(Client client)
        {
            if (MySQLConnected)
            {
                SavePlayerSQL(client);
            }
            else
            {
                SavePlayerXML(client);
            }
        }
        public static int GetUserId(string username)
        {
            if (MySQLConnected)
            {
                return GetUserIdSQL(username);
            }
            else
            {
                return -1;
            }
        }
        public static int CheckPower(string username)
        {
            if (MySQLConnected)
            {
                return CheckPowerSQL(username);
            }
            else
            {
                return CheckPowerXML(username);
            }
        }
        public static Client GetPlayerClient(string username)
        {
            //Try to fetch a player entity by username, online or offline.
            //Check Online First
            for (int i = 0; i < Globals.Clients.Count; i++)
            {
                if (Globals.Clients[i] != null && Globals.Clients[i].IsConnected() && Globals.Clients[i].Entity != null)
                {
                    if (Globals.Clients[i].Entity.MyAccount == username) { return Globals.Clients[i]; }
                }
            }

            //Didn't find the player online, lets load him from our database.
            Client fakeClient = new Client(-1, -1, null);
            Player en = new Player(-1, fakeClient);
            fakeClient.Entity = en;
            en.MyAccount = username;
            fakeClient.Id = GetUserId(username);
            LoadPlayer(fakeClient);
            return fakeClient;
        }
        public static void SetPlayerPower(string username, int power)
        {
            if (AccountExists(username))
            {
                Client player = GetPlayerClient(username);
                player.Power = power;
                SavePlayer(player);
                if (player.ClientIndex > -1)
                {
                    PacketSender.SendPlayerMsg(player, "Your power has been modified!");
                }
                Console.WriteLine(username + "'s power has been set to " + power + "!");
            }
            else
            {
                Console.WriteLine("Account does not exist!");
            }
        }

        //Players_MySQL
        public static bool InitMySql()
        {
            ConnectionString = @"server=" + Globals.MySqlHost + ";userid=" + Globals.MySqlUser + ";"
            + "password=" + Globals.MySqlPass + ";";
            try
            {
                using (var mysqlConn = new MySqlConnection(ConnectionString))
                {
                    mysqlConn.Open();
                    var query = "CREATE SCHEMA IF NOT EXISTS `" + Globals.MySqldb + "`";
                    var cmd = new MySqlCommand(query, mysqlConn);
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception)
            {
                //ignore
            }
            ConnectionString = @"server=" + Globals.MySqlHost + ";userid=" + Globals.MySqlUser + ";"
            + "password=" + Globals.MySqlPass + ";database=" + Globals.MySqldb + ";";
            try
            {
                using (var mysqlConn = new MySqlConnection(ConnectionString))
                {
                    mysqlConn.Open();
                    Globals.GeneralLogs.Add("Connected to MySQL successfully.");
                    Globals.GeneralLogs.Add("Checking table integrity.");
                    CheckTables();
                    Globals.GeneralLogs.Add("Server has " + GetRegisteredPlayersSQL() + " registered players.");
                    return true;
                }
            }
            catch (Exception)
            {
            }
            return false;
        }
        private static void CheckTables()
        {

            using (var mysqlConn = new MySqlConnection(ConnectionString))
            {
                mysqlConn.Open();
                CheckUsersTable(mysqlConn);
                CheckSwitchesTable(mysqlConn);
                CheckVariablesTable(mysqlConn);
                CheckInventoryTable(mysqlConn);
                CheckSpellsTable(mysqlConn);
                CheckHotbarTable(mysqlConn);

            }
        }
        private static void CheckUsersTable(MySqlConnection mysqlConn)
        {
            const string myTable = "users";
            var query = "CREATE TABLE IF NOT EXISTS `" + myTable + "` (`id` int(11) NOT NULL AUTO_INCREMENT,PRIMARY KEY (`id`))";
            var cmd = new MySqlCommand(query, mysqlConn);
            cmd.ExecuteNonQuery();
            query = "SHOW COLUMNS FROM " + myTable + ";";
            cmd = new MySqlCommand(query, mysqlConn);
            var reader = cmd.ExecuteReader();
            var columns = new List<string>();
            while (reader.Read())
            {
                columns.Add(reader.GetValue(0).ToString());
            }
            reader.Close();

            //Work on each field
            CheckTableField(mysqlConn, columns, "user", myTable, MySqlFields.m_string, 45);
            CheckTableField(mysqlConn, columns, "pass", myTable, MySqlFields.m_string, 64);
            CheckTableField(mysqlConn, columns, "salt", myTable, MySqlFields.m_string, 64);
            CheckTableField(mysqlConn, columns, "email", myTable, MySqlFields.m_string, 100);
            CheckTableField(mysqlConn, columns, "name", myTable, MySqlFields.m_string, 45);
            CheckTableField(mysqlConn, columns, "map", myTable, MySqlFields.m_int);
            CheckTableField(mysqlConn, columns, "x", myTable, MySqlFields.m_int);
            CheckTableField(mysqlConn, columns, "y", myTable, MySqlFields.m_int);
            CheckTableField(mysqlConn, columns, "z", myTable, MySqlFields.m_int);
            CheckTableField(mysqlConn, columns, "dir", myTable, MySqlFields.m_int);
            CheckTableField(mysqlConn, columns, "sprite", myTable, MySqlFields.m_string, 45);
            CheckTableField(mysqlConn, columns, "class", myTable, MySqlFields.m_int);
            CheckTableField(mysqlConn, columns, "gender", myTable, MySqlFields.m_int);
            CheckTableField(mysqlConn, columns, "level", myTable, MySqlFields.m_int);
            CheckTableField(mysqlConn, columns, "experience", myTable, MySqlFields.m_int);
            for (var i = 0; i < (int)Enums.Vitals.VitalCount; i++)
            {
                CheckTableField(mysqlConn, columns, "vital" + i, myTable, MySqlFields.m_int);
                CheckTableField(mysqlConn, columns, "maxvital" + i, myTable, MySqlFields.m_int);
            }
            for (var i = 0; i < (int)Enums.Stats.StatCount; i++)
            {
                CheckTableField(mysqlConn, columns, "stat" + i, myTable, MySqlFields.m_int);
            }
            CheckTableField(mysqlConn, columns, "statpoints", myTable, MySqlFields.m_int);
            for (var i = 0; i < Enums.EquipmentSlots.Count; i++)
            {
                CheckTableField(mysqlConn, columns, "equipment" + i, myTable, MySqlFields.m_int);
            }
            CheckTableField(mysqlConn, columns, "power", myTable, MySqlFields.m_int);
            CheckTableField(mysqlConn, columns, "face", myTable, MySqlFields.m_string);
        }
        private static void CheckSwitchesTable(MySqlConnection mysqlConn)
        {
            const string myTable = "switches";
            var query = "CREATE TABLE IF NOT EXISTS `" + myTable + "` (`id` int(11) NOT NULL)";
            var cmd = new MySqlCommand(query, mysqlConn);
            cmd.ExecuteNonQuery();
            query = "SHOW COLUMNS FROM " + myTable + ";";
            cmd = new MySqlCommand(query, mysqlConn);
            var reader = cmd.ExecuteReader();
            var columns = new List<string>();
            while (reader.Read())
            {
                columns.Add(reader.GetValue(0).ToString());
            }
            reader.Close();

            //Work on each field
            CheckTableField(mysqlConn, columns, "switchnum", myTable, MySqlFields.m_int);
            CheckTableField(mysqlConn, columns, "switchval", myTable, MySqlFields.m_int);
        }
        private static void CheckVariablesTable(MySqlConnection mysqlConn)
        {
            const string myTable = "variables";
            var query = "CREATE TABLE IF NOT EXISTS `" + myTable + "` (`id` int(11) NOT NULL)";
            var cmd = new MySqlCommand(query, mysqlConn);
            cmd.ExecuteNonQuery();
            query = "SHOW COLUMNS FROM " + myTable + ";";
            cmd = new MySqlCommand(query, mysqlConn);
            var reader = cmd.ExecuteReader();
            var columns = new List<string>();
            while (reader.Read())
            {
                columns.Add(reader.GetValue(0).ToString());
            }
            reader.Close();

            //Work on each field
            CheckTableField(mysqlConn, columns, "variablenum", myTable, MySqlFields.m_int);
            CheckTableField(mysqlConn, columns, "variableval", myTable, MySqlFields.m_int);
        }
        private static void CheckInventoryTable(MySqlConnection mysqlConn)
        {
            const string myTable = "inventories";
            var query = "CREATE TABLE IF NOT EXISTS `" + myTable + "` (`id` int(11) NOT NULL)";
            var cmd = new MySqlCommand(query, mysqlConn);
            cmd.ExecuteNonQuery();
            query = "SHOW COLUMNS FROM " + myTable + ";";
            cmd = new MySqlCommand(query, mysqlConn);
            var reader = cmd.ExecuteReader();
            var columns = new List<string>();
            while (reader.Read())
            {
                columns.Add(reader.GetValue(0).ToString());
            }
            reader.Close();

            //Work on each field
            CheckTableField(mysqlConn, columns, "slot", myTable, MySqlFields.m_int);
            CheckTableField(mysqlConn, columns, "itemnum", myTable, MySqlFields.m_int);
            CheckTableField(mysqlConn, columns, "itemval", myTable, MySqlFields.m_int);
            for (int i = 0; i < (int)Enums.Stats.StatCount; i++)
            {
                CheckTableField(mysqlConn, columns, "statbuff" + i, myTable, MySqlFields.m_int);
            }
        }
        private static void CheckSpellsTable(MySqlConnection mysqlConn)
        {
            const string myTable = "spells";
            var query = "CREATE TABLE IF NOT EXISTS `" + myTable + "` (`id` int(11) NOT NULL)";
            var cmd = new MySqlCommand(query, mysqlConn);
            cmd.ExecuteNonQuery();
            query = "SHOW COLUMNS FROM " + myTable + ";";
            cmd = new MySqlCommand(query, mysqlConn);
            var reader = cmd.ExecuteReader();
            var columns = new List<string>();
            while (reader.Read())
            {
                columns.Add(reader.GetValue(0).ToString());
            }
            reader.Close();

            //Work on each field
            CheckTableField(mysqlConn, columns, "slot", myTable, MySqlFields.m_int);
            CheckTableField(mysqlConn, columns, "spellnum", myTable, MySqlFields.m_int);
            CheckTableField(mysqlConn, columns, "spellcd", myTable, MySqlFields.m_int);
        }
        private static void CheckHotbarTable(MySqlConnection mysqlConn)
        {
            const string myTable = "hotbar";
            var query = "CREATE TABLE IF NOT EXISTS `" + myTable + "` (`id` int(11) NOT NULL)";
            var cmd = new MySqlCommand(query, mysqlConn);
            cmd.ExecuteNonQuery();
            query = "SHOW COLUMNS FROM " + myTable + ";";
            cmd = new MySqlCommand(query, mysqlConn);
            var reader = cmd.ExecuteReader();
            var columns = new List<string>();
            while (reader.Read())
            {
                columns.Add(reader.GetValue(0).ToString());
            }
            reader.Close();

            //Work on each field
            CheckTableField(mysqlConn, columns, "slot", myTable, MySqlFields.m_int);
            CheckTableField(mysqlConn, columns, "itemtype", myTable, MySqlFields.m_int);
            CheckTableField(mysqlConn, columns, "itemslot", myTable, MySqlFields.m_int);
        }
        private static void CheckTableField(MySqlConnection mysqlConn, List<string> columns, string fieldName, string tableName, MySqlFields fieldType, int fieldLength = -1)
        {
            var query = "";
            MySqlCommand cmd;
            if (columns.Contains(fieldName)) { return; }
            switch (fieldType)
            {
                case MySqlFields.m_string:
                    if (fieldLength <= 0) { fieldLength = 100; }
                    query = "ALTER TABLE `" + tableName + "` ADD " + fieldName + " varchar(" + fieldLength + ");";
                    cmd = new MySqlCommand(query, mysqlConn);
                    cmd.ExecuteNonQuery();
                    break;
                case MySqlFields.m_int:
                    if (fieldLength <= 0) { fieldLength = 11; }
                    query = "ALTER TABLE `" + tableName + "` ADD " + fieldName + " int(" + fieldLength + ");";
                    cmd = new MySqlCommand(query, mysqlConn);
                    cmd.ExecuteNonQuery();
                    break;
            }

        }
        public static bool AccountExistsSQL(string accountname)
        {
            var stm = "SELECT COUNT(*) from Users WHERE user='" + accountname.ToLower() + "'";
            using (var mysqlConn = new MySqlConnection(ConnectionString))
            {
                mysqlConn.Open();

                var cmd = new MySqlCommand(stm, mysqlConn);
                var reader = cmd.ExecuteReader();
                var count = 0;
                while (reader.Read())
                {
                    count = reader.GetInt32(0);

                }
                reader.Close();
                if (count > 0)
                {
                    return true;
                }
            }
            return false;
        }
        public static bool EmailInUseSQL(string email)
        {
            var stm = "SELECT COUNT(*) from Users WHERE email='" + email.ToLower() + "'";
            using (var mysqlConn = new MySqlConnection(ConnectionString))
            {
                mysqlConn.Open();
                var cmd = new MySqlCommand(stm, mysqlConn);
                var reader = cmd.ExecuteReader();
                var count = 0;
                while (reader.Read())
                {
                    count = reader.GetInt32(0);
                }
                reader.Close();
                if (count > 0)
                {
                    return true;
                }
            }
            return false;
        }
        public static bool CharacterNameInUseSQL(string name)
        {
            var stm = "SELECT COUNT(*) from Users WHERE name='" + name.ToLower() + "'";
            using (var mysqlConn = new MySqlConnection(ConnectionString))
            {
                mysqlConn.Open();
                var cmd = new MySqlCommand(stm, mysqlConn);
                var reader = cmd.ExecuteReader();
                var count = 0;
                while (reader.Read())
                {
                    count = reader.GetInt32(0);
                }
                reader.Close();
                if (count > 0)
                {
                    return true;
                }
            }
            return false;
        }
        private static int GetRegisteredPlayersSQL()
        {
            const string query = "SELECT COUNT(*) from Users";
            using (var mysqlConn = new MySqlConnection(ConnectionString))
            {
                mysqlConn.Open();
                var cmd = new MySqlCommand(query, mysqlConn);
                var reader = cmd.ExecuteReader();
                var result = 0;
                while (reader.Read())
                {
                    result = reader.GetInt32(0);
                }
                reader.Close();
                return result;
            }
        }
        public static void CreateAccountSQL(Player en, string username, string password, string email)
        {
            var sha = new SHA256Managed();
            en.MyAccount = username;

            //Generate a Salt
            RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();
            byte[] buff = new byte[20];
            rng.GetBytes(buff);
            en.MySalt = BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(Convert.ToBase64String(buff)))).Replace("-", "");

            //Hash the Password
            en.MyPassword = BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(password + en.MySalt))).Replace("-", "");

            en.MyEmail = email;
            using (var mysqlConn = new MySqlConnection(ConnectionString))
            {
                mysqlConn.Open();
                var stm = "INSERT INTO Users (user,pass,salt,email) VALUES ('" + username.ToLower() + "','" + en.MyPassword + "','" + en.MySalt + "','" + email.ToLower() + "');";
                var cmd = new MySqlCommand(stm, mysqlConn);
                cmd.ExecuteNonQuery();
            }
        }
        public static bool CheckPasswordSQL(string username, string password)
        {
            var sha = new SHA256Managed();
            var stm = "SELECT pass,salt FROM Users WHERE user = '" + username + "'";
            using (var mysqlConn = new MySqlConnection(ConnectionString))
            {
                mysqlConn.Open();
                var cmd = new MySqlCommand(stm, mysqlConn);
                var reader = cmd.ExecuteReader();
                var result = false;
                while (reader.Read())
                {
                    string pass = reader.GetString(0);
                    string salt = reader.GetString(1);
                    string temppass = BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(password + salt))).Replace("-", "");
                    if (temppass == pass) { result = true; }
                }
                reader.Close();
                return result;
            }
        }
        public static int GetUserIdSQL(string username)
        {
            var stm = "SELECT id FROM Users WHERE user = '" + username + "'";
            using (var mysqlConn = new MySqlConnection(ConnectionString))
            {
                mysqlConn.Open();
                var cmd = new MySqlCommand(stm, mysqlConn);
                var reader = cmd.ExecuteReader();
                var result = -1;
                while (reader.Read())
                {
                    result = reader.GetInt32(0);
                }
                reader.Close();
                return result;
            }
        }
        public static int CheckPowerSQL(string username)
        {
            var stm = "SELECT power FROM Users WHERE user = '" + username + "'";
            using (var mysqlConn = new MySqlConnection(ConnectionString))
            {
                mysqlConn.Open();
                var cmd = new MySqlCommand(stm, mysqlConn);
                var reader = cmd.ExecuteReader();
                var result = -1;
                while (reader.Read())
                {
                    result = reader.GetInt32(0);
                }
                reader.Close();
                return result;
            }
        }
        public static bool LoadPlayerSQL(Client client)
        {
            var stm = "SELECT * FROM Users WHERE id = " + client.Id + "";
            using (var mysqlConn = new MySqlConnection(ConnectionString))
            {
                mysqlConn.Open();
                var cmd = new MySqlCommand(stm, mysqlConn);
                var reader = cmd.ExecuteReader();
                var en = client.Entity;
                var columns = Enumerable.Range(0, reader.FieldCount).Select(reader.GetName).ToList();
                int i;
                try
                {
                    while (reader.Read())
                    {
                        en.MyName = reader.GetString(columns.IndexOf("name"));
                        en.CurrentMap = reader.GetInt32(columns.IndexOf("map"));
                        en.CurrentX = reader.GetInt32(columns.IndexOf("x"));
                        en.CurrentY = reader.GetInt32(columns.IndexOf("y"));
                        en.CurrentZ = reader.GetInt32(columns.IndexOf("z"));
                        en.Dir = reader.GetInt32(columns.IndexOf("dir"));
                        en.MySprite = reader.GetString(columns.IndexOf("sprite"));
                        en.Class = reader.GetInt32(columns.IndexOf("class"));
                        en.Gender = reader.GetInt32(columns.IndexOf("gender"));
                        en.Level = reader.GetInt32(columns.IndexOf("level"));
                        en.Experience = reader.GetInt32(columns.IndexOf("experience"));
                        for (i = 0; i < (int)Enums.Vitals.VitalCount; i++)
                        {
                            en.Vital[i] = reader.GetInt32(columns.IndexOf("vital" + i));
                            en.MaxVital[i] = reader.GetInt32(columns.IndexOf("maxvital" + i));
                        }
                        for (i = 0; i < (int)Enums.Stats.StatCount; i++)
                        {
                            en.Stat[i].Stat = reader.GetInt32(columns.IndexOf("stat" + i));
                        }
                        en.StatPoints = reader.GetInt32(columns.IndexOf("statpoints"));
                        for (i = 0; i < Enums.EquipmentSlots.Count; i++)
                        {
                            en.Equipment[i] = reader.GetInt32(columns.IndexOf("equipment" + i));
                        }
                        client.Power = reader.GetInt32(columns.IndexOf("power"));
                        en.Face = reader.GetString(columns.IndexOf("face"));
                    }
                    reader.Close();

                    i = 0;
                    stm = "SELECT switchval from Switches WHERE id=" + client.Id + ";";
                    cmd = new MySqlCommand(stm, mysqlConn);
                    reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        if (i >= Constants.MaxPlayerSwitches) continue;
                        ((Player)en).Switches[i] = Convert.ToBoolean(reader.GetInt32(0));
                        i++;
                    }
                    reader.Close();
                    i = 0;
                    stm = "SELECT variableval from Variables WHERE id=" + client.Id + ";";
                    cmd = new MySqlCommand(stm, mysqlConn);
                    reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        if (i >= Constants.MaxPlayerVariables) continue;
                        ((Player)en).Variables[i] = reader.GetInt32(0);
                        i++;
                    }
                    reader.Close();
                    i = 0;
                    stm = "SELECT * from Inventories WHERE id=" + client.Id + ";";
                    cmd = new MySqlCommand(stm, mysqlConn);
                    reader = cmd.ExecuteReader();
                    columns = Enumerable.Range(0, reader.FieldCount).Select(reader.GetName).ToList();
                    while (reader.Read())
                    {
                        if (reader.GetInt32(columns.IndexOf("slot")) < Constants.MaxInvItems)
                        {
                            ((Player)en).Inventory[reader.GetInt32(columns.IndexOf("slot"))].ItemNum = reader.GetInt32(columns.IndexOf("itemnum"));
                            ((Player)en).Inventory[reader.GetInt32(columns.IndexOf("slot"))].ItemVal = reader.GetInt32(columns.IndexOf("itemval"));
                            for (int x = 0; x < (int)Enums.Stats.StatCount; x++)
                            {
                                ((Player)en).Inventory[reader.GetInt32(columns.IndexOf("slot"))].StatBoost[x] = reader.GetInt32(columns.IndexOf("statbuff" + x));
                            }
                        }
                    }
                    reader.Close();
                    i = 0;
                    stm = "SELECT * from Spells WHERE id=" + client.Id + ";";
                    cmd = new MySqlCommand(stm, mysqlConn);
                    reader = cmd.ExecuteReader();
                    columns = Enumerable.Range(0, reader.FieldCount).Select(reader.GetName).ToList();
                    while (reader.Read())
                    {
                        if (reader.GetInt32(columns.IndexOf("slot")) < Constants.MaxPlayerSkills)
                        {
                            ((Player)en).Spells[reader.GetInt32(columns.IndexOf("slot"))].SpellNum = reader.GetInt32(columns.IndexOf("spellnum"));
                            ((Player)en).Spells[reader.GetInt32(columns.IndexOf("slot"))].SpellCD = 0;
                        }
                    }
                    reader.Close();
                    i = 0;
                    stm = "SELECT * from hotbar WHERE id=" + client.Id + ";";
                    cmd = new MySqlCommand(stm, mysqlConn);
                    reader = cmd.ExecuteReader();
                    columns = Enumerable.Range(0, reader.FieldCount).Select(reader.GetName).ToList();
                    while (reader.Read())
                    {
                        if (reader.GetInt32(columns.IndexOf("slot")) < Constants.MaxHotbar)
                        {
                            ((Player)en).Hotbar[reader.GetInt32(columns.IndexOf("slot"))].Type = reader.GetInt32(columns.IndexOf("itemtype"));
                            ((Player)en).Hotbar[reader.GetInt32(columns.IndexOf("slot"))].Slot = reader.GetInt32(columns.IndexOf("itemslot"));
                        }
                    }
                    reader.Close();
                }
                catch (Exception ex)
                {
                    return false;
                }
                return true;
            }
        }
        public static void SavePlayerSQL(Client client)
        {
            if (client == null) { return; }
            if (client.Entity == null) { return; }
            var en = (Player)client.Entity;
            var query = "UPDATE Users SET ";
            var id = GetUserId(en.MyAccount);
            query += "name='" + en.MyName + "',";
            query += "map=" + en.CurrentMap + ",";
            query += "x=" + en.CurrentX + ",";
            query += "y=" + en.CurrentY + ",";
            query += "z=" + en.CurrentZ + ",";
            query += "dir=" + en.Dir + ",";
            query += "sprite='" + en.MySprite + "',";
            query += "class=" + en.Class + ",";
            query += "gender=" + en.Gender + ",";
            query += "level=" + en.Level + ",";
            query += "experience=" + en.Experience + ",";
            for (var i = 0; i < (int)Enums.Vitals.VitalCount; i++)
            {
                query += "vital" + i + "=" + en.Vital[i] + ",";
                query += "maxvital" + i + "=" + en.MaxVital[i] + ",";
            }
            for (var i = 0; i < (int)Enums.Stats.StatCount; i++)
            {
                query += "stat" + i + "=" + en.Stat[i] + ",";
            }
            query += "statpoints=" + client.Entity.StatPoints + ",";
            for (var i = 0; i < Enums.EquipmentSlots.Count; i++)
            {
                query += "equipment" + i + "=" + en.Equipment[i] + ",";
            }
            query += "power=" + client.Power + ",";
            query += "face='" + en.Face + "' ";
            query += " WHERE user='" + en.MyName + "'";
            using (var mysqlConn = new MySqlConnection(ConnectionString))
            {
                mysqlConn.Open();
                var cmd = new MySqlCommand(query, mysqlConn);
                cmd.ExecuteNonQuery();

                //Save Switches
                query = "SELECT COUNT(*) from Switches WHERE id=" + id;
                cmd = new MySqlCommand(query, mysqlConn);
                var reader = cmd.ExecuteReader();
                var result = 0;
                while (reader.Read())
                {
                    result = reader.GetInt32(0);
                }
                reader.Close();
                if (result < Constants.MaxPlayerSwitches)
                {
                    query = "";
                    for (var i = result; i < Constants.MaxPlayerSwitches; i++)
                    {
                        query += "INSERT INTO Switches (id,switchnum,switchval) VALUES (" + id + "," + i + ",0);\n";
                    }
                    cmd = new MySqlCommand(query, mysqlConn);
                    cmd.ExecuteNonQuery();
                }
                query = "";
                for (var i = 0; i < Constants.MaxPlayerSwitches; i++)
                {
                    query += "UPDATE Switches SET switchval=" + Convert.ToInt32(((Player)(en)).Switches[i]) + " WHERE id=" + id + " AND switchnum=" + i + ";\n";
                }
                cmd = new MySqlCommand(query, mysqlConn);
                cmd.ExecuteNonQuery();

                //Save Variables
                query = "SELECT COUNT(*) from Variables WHERE id=" + id;
                cmd = new MySqlCommand(query, mysqlConn);
                reader = cmd.ExecuteReader();
                result = 0;
                while (reader.Read())
                {
                    result = reader.GetInt32(0);
                }
                reader.Close();
                if (result < Constants.MaxPlayerVariables)
                {
                    query = "";
                    for (var i = result; i < Constants.MaxPlayerVariables; i++)
                    {
                        query += "INSERT INTO Variables (id,variablenum,variableval) VALUES (" + id + "," + i + ",0);\n";
                    }
                    cmd = new MySqlCommand(query, mysqlConn);
                    cmd.ExecuteNonQuery();
                }
                query = "";
                for (var i = 0; i < Constants.MaxPlayerVariables; i++)
                {
                    query += "UPDATE Variables SET variableval=" + Convert.ToInt32(((Player)(en)).Variables[i]) + " WHERE id=" + id + " AND variablenum=" + i + ";\n";
                }
                cmd = new MySqlCommand(query, mysqlConn);
                cmd.ExecuteNonQuery();

                //Save Inventory
                query = "SELECT COUNT(*) from Inventories WHERE id=" + id;
                cmd = new MySqlCommand(query, mysqlConn);
                reader = cmd.ExecuteReader();
                result = 0;
                while (reader.Read())
                {
                    result = reader.GetInt32(0);
                }
                reader.Close();
                if (result < Constants.MaxInvItems)
                {
                    query = "";
                    for (var i = result; i < Constants.MaxInvItems; i++)
                    {
                        query += "INSERT INTO Inventories (id,slot,itemnum,itemval";
                        for (int x = 0; x < (int)Enums.Stats.StatCount; x++)
                        {
                            query += ", statbuff" + x;
                        }
                        query += ") VALUES (" + id + "," + i + ",-1,0";
                        for (int x = 0; x < (int)Enums.Stats.StatCount; x++)
                        {
                            query += ", 0";
                        }
                        query += ");\n";
                    }
                    cmd = new MySqlCommand(query, mysqlConn);
                    cmd.ExecuteNonQuery();
                }
                query = "";
                for (var i = 0; i < Constants.MaxInvItems; i++)
                {
                    query += "UPDATE Inventories SET itemnum=" + en.Inventory[i].ItemNum + ", itemval=" + en.Inventory[i].ItemVal;
                    for (int x = 0; x < (int)Enums.Stats.StatCount; x++)
                    {
                        query += ", statbuff" + x + "=" + en.Inventory[i].StatBoost[x];
                    }
                    query += " WHERE id=" + id + " AND slot=" + i + ";\n";
                }
                cmd = new MySqlCommand(query, mysqlConn);
                cmd.ExecuteNonQuery();

                //Save Spells
                query = "SELECT COUNT(*) from Spells WHERE id=" + id;
                cmd = new MySqlCommand(query, mysqlConn);
                reader = cmd.ExecuteReader();
                result = 0;
                while (reader.Read())
                {
                    result = reader.GetInt32(0);
                }
                reader.Close();
                if (result < Constants.MaxPlayerSkills)
                {
                    query = "";
                    for (var i = result; i < Constants.MaxPlayerSkills; i++)
                    {
                        query += "INSERT INTO Spells (id,slot,spellnum,spellcd) VALUES (" + id + "," + i + ",-1,0);\n";
                    }
                    cmd = new MySqlCommand(query, mysqlConn);
                    cmd.ExecuteNonQuery();
                }
                query = "";
                for (var i = 0; i < Constants.MaxPlayerSkills; i++)
                {
                    query += "UPDATE Spells SET spellnum=" + en.Spells[i].SpellNum + ", spellcd=" + 0 + " WHERE id=" + id + " AND slot=" + i + ";\n";
                }
                cmd = new MySqlCommand(query, mysqlConn);
                cmd.ExecuteNonQuery();

                //Save Hotbar Slots
                query = "SELECT COUNT(*) from hotbar WHERE id=" + id;
                cmd = new MySqlCommand(query, mysqlConn);
                reader = cmd.ExecuteReader();
                result = 0;
                while (reader.Read())
                {
                    result = reader.GetInt32(0);
                }
                reader.Close();
                if (result < Constants.MaxHotbar)
                {
                    query = "";
                    for (var i = result; i < Constants.MaxPlayerSkills; i++)
                    {
                        query += "INSERT INTO hotbar (id,slot,itemtype,itemslot) VALUES (" + id + "," + i + ",-1,-1);\n";
                    }
                    cmd = new MySqlCommand(query, mysqlConn);
                    cmd.ExecuteNonQuery();
                }
                query = "";
                for (var i = 0; i < Constants.MaxHotbar; i++)
                {
                    query += "UPDATE hotbar SET itemtype=" + en.Hotbar[i].Type + ", itemslot=" + en.Hotbar[i].Slot + " WHERE id=" + id + " AND slot=" + i + ";\n";
                }
                cmd = new MySqlCommand(query, mysqlConn);
                cmd.ExecuteNonQuery();
            }
        }

        //Players_XML
        public static void LoadAccounts()
        {
            string[] accounts = Directory.GetDirectories("resources\\accounts");
            for (int i = 0; i < accounts.Length; i++)
            {
                var playerdata = new XmlDocument();
                playerdata.Load(accounts[i] + "\\" + accounts[i].Replace("resources\\accounts", "") + ".xml");
                Accounts.Add(playerdata.SelectSingleNode("//PlayerData/Username").InnerText);
                Emails.Add(playerdata.SelectSingleNode("//PlayerData/Email").InnerText);
                Characters.Add(playerdata.SelectSingleNode("//PlayerData//CharacterInfo/Name").InnerText);
            }
        }
        public static bool AccountExistsXML(string accountname)
        {
            if (Accounts.IndexOf(accountname) > -1) { return true; }
            return false;
        }
        public static bool EmailInUseXML(string email)
        {
            if (Emails.IndexOf(email) > -1) { return true; }
            return false;
        }
        public static bool CharacterNameInUseXML(string name)
        {
            if (Characters.IndexOf(name) > -1) { return true; }
            return false;
        }
        private static int GetRegisteredPlayersXML()
        {
            return Accounts.Count;
        }
        public static void CreateAccountXML(Player en, string username, string password, string email)
        {
            var sha = new SHA256Managed();
            Directory.CreateDirectory("resources\\accounts\\" + username);
            en.MyAccount = username;

            //Generate a Salt
            RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();
            byte[] buff = new byte[20];
            rng.GetBytes(buff);
            en.MySalt = BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(Convert.ToBase64String(buff)))).Replace("-", "");

            //Hash the Password
            en.MyPassword = BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(password + en.MySalt))).Replace("-", "");

            en.MyEmail = email;
            Accounts.Add(username);
            Emails.Add(email);
        }
        public static bool CheckPasswordXML(string username, string password)
        {
            var playerdata = new XmlDocument();
            var sha = new SHA256Managed();
            playerdata.Load("resources\\accounts\\" + username + "\\" + username + ".xml");
            string salt = playerdata.SelectSingleNode("//PlayerData/Salt").InnerText;
            string pass = playerdata.SelectSingleNode("//PlayerData/Pass").InnerText;
            string temppass = BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(password + salt))).Replace("-", "");
            if (temppass == pass) { return true; }
            return false;
        }
        public static bool LoadPlayerXML(Client client)
        {
            var en = client.Entity;
            try
            {
                var playerdata = new XmlDocument();
                playerdata.Load("resources\\accounts\\" + ((Player)en).MyAccount + "\\" + ((Player)en).MyAccount + ".xml");
                en.MyEmail = playerdata.SelectSingleNode("//PlayerData/Email").InnerText;
                en.MySalt = playerdata.SelectSingleNode("//PlayerData/Salt").InnerText;
                en.MyPassword = playerdata.SelectSingleNode("//PlayerData/Pass").InnerText;

                en.MyName = playerdata.SelectSingleNode("//PlayerData//CharacterInfo/Name").InnerText;
                en.CurrentMap = Int32.Parse(playerdata.SelectSingleNode("//PlayerData//CharacterInfo/Map").InnerText);
                en.CurrentX = Int32.Parse(playerdata.SelectSingleNode("//PlayerData//CharacterInfo/X").InnerText);
                en.CurrentY = Int32.Parse(playerdata.SelectSingleNode("//PlayerData//CharacterInfo/Y").InnerText);
                en.CurrentZ = Int32.Parse(playerdata.SelectSingleNode("//PlayerData//CharacterInfo/Z").InnerText);
                en.Dir = Int32.Parse(playerdata.SelectSingleNode("//PlayerData//CharacterInfo/Dir").InnerText);
                en.MySprite = playerdata.SelectSingleNode("//PlayerData//CharacterInfo/Sprite").InnerText;
                en.Class = Int32.Parse(playerdata.SelectSingleNode("//PlayerData//CharacterInfo/Class").InnerText);
                en.Gender = Int32.Parse(playerdata.SelectSingleNode("//PlayerData//CharacterInfo/Gender").InnerText);
                en.Level = Int32.Parse(playerdata.SelectSingleNode("//PlayerData//CharacterInfo/Level").InnerText);
                en.Experience = Int32.Parse(playerdata.SelectSingleNode("//PlayerData//CharacterInfo/Experience").InnerText);
                for (var i = 0; i < (int)Enums.Vitals.VitalCount; i++)
                {
                    en.Vital[i] = Int32.Parse(playerdata.SelectSingleNode("//PlayerData//CharacterInfo/Vital" + i).InnerText);
                    en.MaxVital[i] = Int32.Parse(playerdata.SelectSingleNode("//PlayerData//CharacterInfo/MaxVital" + i).InnerText);
                }
                for (var i = 0; i < (int)Enums.Stats.StatCount; i++)
                {
                    en.Stat[i].Stat = Int32.Parse(playerdata.SelectSingleNode("//PlayerData//CharacterInfo/Stat" + i).InnerText);
                }
                en.StatPoints = Int32.Parse(playerdata.SelectSingleNode("//PlayerData//CharacterInfo/StatPoints").InnerText);
                for (var i = 0; i < Enums.EquipmentSlots.Count; i++)
                {
                    en.Equipment[i] = Int32.Parse(playerdata.SelectSingleNode("//PlayerData//CharacterInfo/Equipment" + i).InnerText);
                }
                client.Power = Int32.Parse(playerdata.SelectSingleNode("//PlayerData//CharacterInfo/Power").InnerText);
                en.Face = playerdata.SelectSingleNode("//PlayerData//CharacterInfo/Face").InnerText;

                for (int i = 0; i < Constants.MaxPlayerSwitches; i++)
                {
                    ((Player)(en)).Switches[i] = Convert.ToBoolean(Int32.Parse(playerdata.SelectSingleNode("//PlayerData//CharacterInfo//Switches/Switch" + i).InnerText));
                }

                for (int i = 0; i < Constants.MaxPlayerSwitches; i++)
                {
                    ((Player)(en)).Variables[i] = Int32.Parse(playerdata.SelectSingleNode("//PlayerData//CharacterInfo//Variables/Variable" + i).InnerText);
                }

                for (int i = 0; i < Constants.MaxInvItems; i++)
                {
                    en.Inventory[i].ItemNum = Int32.Parse(playerdata.SelectSingleNode("//PlayerData//CharacterInfo//Inventory/Slot" + i + "Num").InnerText);
                    en.Inventory[i].ItemVal = Int32.Parse(playerdata.SelectSingleNode("//PlayerData//CharacterInfo//Inventory/Slot" + i + "Val").InnerText);
                    for (int x = 0; x < (int)Enums.Stats.StatCount; x++)
                    {
                        en.Inventory[i].StatBoost[x] = Int32.Parse(playerdata.SelectSingleNode("//PlayerData//CharacterInfo//Inventory/Slot" + i + "Buff" + x).InnerText);
                    }
                }

                for (int i = 0; i < Constants.MaxPlayerSkills; i++)
                {
                    en.Spells[i].SpellNum = Int32.Parse(playerdata.SelectSingleNode("//PlayerData//CharacterInfo//Spells/Slot" + i + "Num").InnerText);
                }

                for (int i = 0; i < Constants.MaxHotbar; i++)
                {
                    en.Hotbar[i].Type = Int32.Parse(playerdata.SelectSingleNode("//PlayerData//CharacterInfo//Hotbar/Slot" + i + "Type").InnerText);
                    en.Hotbar[i].Slot = Int32.Parse(playerdata.SelectSingleNode("//PlayerData//CharacterInfo//Hotbar/Slot" + i + "Slot").InnerText);
                }

                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        public static void SavePlayerXML(Client client)
        {
            if (client == null) { return; }
            if (client.Entity == null) { return; }
            var en = (Player)client.Entity;

            var playerdata = new XmlWriterSettings { Indent = true };
            playerdata.ConformanceLevel = ConformanceLevel.Auto;
            var writer = XmlWriter.Create("resources\\accounts\\" + ((Player)en).MyAccount + "\\" + ((Player)en).MyAccount + ".xml", playerdata);
            writer.WriteStartDocument();
            writer.WriteStartElement("PlayerData");
            writer.WriteElementString("Username", ((Player)en).MyAccount);
            writer.WriteElementString("Email", ((Player)en).MyEmail);
            writer.WriteElementString("Pass", ((Player)en).MyPassword);
            writer.WriteElementString("Salt", ((Player)en).MySalt);

            writer.WriteStartElement("CharacterInfo");
            writer.WriteElementString("Name", en.MyName);
            writer.WriteElementString("Map", en.CurrentMap.ToString());
            writer.WriteElementString("X", en.CurrentX.ToString());
            writer.WriteElementString("Y", en.CurrentY.ToString());
            writer.WriteElementString("Z", en.CurrentZ.ToString());
            writer.WriteElementString("Dir", en.Dir.ToString());
            writer.WriteElementString("Sprite", en.MySprite);
            writer.WriteElementString("Class", en.Class.ToString());
            writer.WriteElementString("Gender", en.Gender.ToString());
            writer.WriteElementString("Level", en.Level.ToString());
            writer.WriteElementString("Experience", en.Experience.ToString());
            for (var i = 0; i < (int)Enums.Vitals.VitalCount; i++)
            {
                writer.WriteElementString("Vital" + i, en.Vital[i].ToString());
                writer.WriteElementString("MaxVital" + i, en.MaxVital[i].ToString());
            }
            for (var i = 0; i < (int)Enums.Stats.StatCount; i++)
            {
                writer.WriteElementString("Stat" + i, en.Stat[i].Stat.ToString());
            }
            writer.WriteElementString("StatPoints", en.StatPoints.ToString());
            for (var i = 0; i < Enums.EquipmentSlots.Count; i++)
            {
                writer.WriteElementString("Equipment" + i, en.Equipment[i].ToString());
            }
            writer.WriteElementString("Power", client.Power.ToString());
            writer.WriteElementString("Face", en.Face);

            writer.WriteStartElement("Switches");
            for (int i = 0; i < Constants.MaxPlayerSwitches; i++)
            {
                writer.WriteElementString("Switch" + i, Convert.ToInt32(((Player)(en)).Switches[i]).ToString());
            }
            writer.WriteEndElement();

            writer.WriteStartElement("Variables");
            for (int i = 0; i < Constants.MaxPlayerVariables; i++)
            {
                writer.WriteElementString("Variable" + i, ((Player)(en)).Variables[i].ToString());
            }
            writer.WriteEndElement();

            writer.WriteStartElement("Inventory");
            for (int i = 0; i < Constants.MaxInvItems; i++)
            {
                writer.WriteElementString("Slot" + i + "Num", en.Inventory[i].ItemNum.ToString());
                writer.WriteElementString("Slot" + i + "Val", en.Inventory[i].ItemVal.ToString());
                for (int x = 0; x < (int)Enums.Stats.StatCount; x++)
                {
                    writer.WriteElementString("Slot" + i + "Buff" + x, en.Inventory[i].StatBoost[x].ToString());
                }

            }
            writer.WriteEndElement();

            writer.WriteStartElement("Spells");
            for (int i = 0; i < Constants.MaxPlayerSkills; i++)
            {
                writer.WriteElementString("Slot" + i + "Num", en.Spells[i].SpellNum.ToString());
            }
            writer.WriteEndElement();


            writer.WriteStartElement("Hotbar");
            for (int i = 0; i < Constants.MaxHotbar; i++)
            {
                writer.WriteElementString("Slot" + i + "Type", en.Hotbar[i].Type.ToString());
                writer.WriteElementString("Slot" + i + "Slot", en.Hotbar[i].Slot.ToString());
            }
            writer.WriteEndElement();



            writer.WriteEndElement();
            writer.WriteEndElement();
            writer.WriteEndDocument();
            writer.Flush();
            writer.Close();
        }
        public static int CheckPowerXML(string username)
        {
            var playerdata = new XmlDocument();
            var sha = new SHA256Managed();
            playerdata.Load("resources\\accounts\\" + username + "\\" + username + ".xml");
            int power = Int32.Parse(playerdata.SelectSingleNode("//PlayerData//CharacterInfo/Power").InnerText);
            return power;
        }

        //Maps
        public static void LoadMaps()
        {
            if (!Directory.Exists("Resources/Maps"))
            {
                Directory.CreateDirectory("Resources/Maps");
            }
            var mapNames = Directory.GetFiles("Resources/Maps", "*.map");
            Globals.MapCount = mapNames.Length;
            Globals.GameMaps = new MapStruct[mapNames.Length];
            if (Globals.MapCount == 0)
            {
                Console.WriteLine("No maps found! - Creating empty first map!");
                Globals.MapCount = 1;
                Globals.GameMaps = new MapStruct[1];
                Globals.GameMaps[0] = new MapStruct(0);
                Globals.GameMaps[0].Save(true);
            }
            else
            {
                for (var i = 0; i < mapNames.Length; i++)
                {
                    Globals.GameMaps[i] = new MapStruct(i);
                    if (!Globals.GameMaps[i].Load(File.ReadAllBytes("Resources/Maps/" + i + ".map")))
                        Globals.GameMaps[i] = null;

                }
            }
            GenerateMapGrids();
            LoadMapFolders();
            CheckAllMapConnections();
        }
        public static void CheckAllMapConnections()
        {
            for (int i = 0; i < Globals.GameMaps.Length; i++)
            {
                if (MapHelper.IsMapValid(i))
                {
                    CheckMapConnections(i);
                }
            }
        }
        public static void CheckMapConnections(int mapNum)
        {
            bool updated = false;
            if (!MapHelper.IsMapValid(Globals.GameMaps[mapNum].Up)) { Globals.GameMaps[mapNum].Up = -1; updated = true; }
            if (!MapHelper.IsMapValid(Globals.GameMaps[mapNum].Down)) { Globals.GameMaps[mapNum].Down = -1; updated = true; }
            if (!MapHelper.IsMapValid(Globals.GameMaps[mapNum].Left)) { Globals.GameMaps[mapNum].Left = -1; updated = true; }
            if (!MapHelper.IsMapValid(Globals.GameMaps[mapNum].Right)) { Globals.GameMaps[mapNum].Right = -1; updated = true; }
            if (updated)
            {
                Globals.GameMaps[mapNum].Save();
                PacketSender.SendMapToEditors(mapNum);
            }
        }
        public static void GenerateMapGrids()
        {
            lock (MapGridLock)
            {
                MapGrids.Clear();
                for (var i = 0; i < Globals.MapCount; i++)
                {
                    if (Globals.GameMaps[i] == null) continue;
                    if (!MapGrids.Any())
                    {
                        MapGrids.Add(new MapGrid(i, 0));
                    }
                    else
                    {
                        for (var y = 0; y < MapGrids.Count(); y++)
                        {
                            if (!MapGrids[y].HasMap(i))
                            {
                                if (y != MapGrids.Count() - 1) continue;
                                MapGrids.Add(new MapGrid(i, MapGrids.Count()));
                                break;
                            }
                            else
                            {
                                break;
                            }
                        }
                    }
                }
                for (var i = 0; i < Globals.MapCount; i++)
                {
                    if (Globals.GameMaps[i] == null) continue;
                    Globals.GameMaps[i].SurroundingMaps.Clear();
                    var myGrid = Globals.GameMaps[i].MapGrid;
                    for (var x = Globals.GameMaps[i].MapGridX - 1; x <= Globals.GameMaps[i].MapGridX + 1; x++)
                    {
                        for (var y = Globals.GameMaps[i].MapGridY - 1; y <= Globals.GameMaps[i].MapGridY + 1; y++)
                        {
                            if ((x == Globals.GameMaps[i].MapGridX) && (y == Globals.GameMaps[i].MapGridY))
                                continue;
                            if (x >= MapGrids[myGrid].XMin && x < MapGrids[myGrid].XMax && y >= MapGrids[myGrid].YMin &&
                                y < MapGrids[myGrid].YMax && MapGrids[myGrid].MyGrid[x, y] > -1)
                            {
                                Globals.GameMaps[i].SurroundingMaps.Add(MapGrids[myGrid].MyGrid[x, y]);
                            }
                        }
                    }
                }
            }
        }
        public static int AddMap()
        {
            var tmpMaps = (MapStruct[])Globals.GameMaps.Clone();
            Globals.MapCount++;
            Globals.GameMaps = new MapStruct[Globals.MapCount];
            tmpMaps.CopyTo(Globals.GameMaps, 0);
            Globals.GameMaps[Globals.MapCount - 1] = new MapStruct(Globals.MapCount - 1);
            Globals.GameMaps[Globals.MapCount - 1].Save(true);
            return Globals.MapCount - 1;
        }
        public static void LoadNpcs()
        {
            if (!Directory.Exists("Resources/Npcs"))
            {
                Directory.CreateDirectory("Resources/Npcs");
            }
            Globals.GameNpcs = new NpcStruct[Constants.MaxNpcs];
            for (var i = 0; i < Constants.MaxNpcs; i++)
            {
                Globals.GameNpcs[i] = new NpcStruct();
                if (!File.Exists("Resources/Npcs/" + i + ".npc"))
                {
                    Globals.GameNpcs[i].Save(i);
                }
                else
                {
                    Globals.GameNpcs[i].Load(File.ReadAllBytes("Resources/Npcs/" + i + ".npc"), i);
                }

            }
        }

        //Items
        public static void LoadItems()
        {
            if (!Directory.Exists("Resources/Items"))
            {
                Directory.CreateDirectory("Resources/Items");
            }

            Globals.GameItems = new ItemStruct[Constants.MaxItems];
            for (var i = 0; i < Constants.MaxItems; i++)
            {
                Globals.GameItems[i] = new ItemStruct();
                if (!File.Exists("Resources/Items/" + i + ".item"))
                {
                    Globals.GameItems[i].Save(i);
                }
                else
                {
                    Globals.GameItems[i].Load(File.ReadAllBytes("Resources/Items/" + i + ".item"), i);
                }
            }
        }

        //Shops
        public static void LoadShops()
        {
            if (!Directory.Exists("Resources/Shops"))
            {
                Directory.CreateDirectory("Resources/Shops");
            }

            Globals.GameShops = new ShopStruct[Constants.MaxShops];
            for (var i = 0; i < Constants.MaxShops; i++)
            {
                Globals.GameShops[i] = new ShopStruct();
                if (!File.Exists("Resources/Shops/" + i + ".shop"))
                {
                    Globals.GameShops[i].Save(i);
                }
                else
                {
                    Globals.GameShops[i].Load(File.ReadAllBytes("Resources/Shops/" + i + ".shop"), i);
                }
            }
        }

        //Spells
        public static void LoadSpells()
        {
            if (!Directory.Exists("Resources/Spells"))
            {
                Directory.CreateDirectory("Resources/Spells");
            }

            Globals.GameSpells = new SpellStruct[Constants.MaxSpells];
            for (var i = 0; i < Constants.MaxSpells; i++)
            {
                Globals.GameSpells[i] = new SpellStruct();
                if (!File.Exists("Resources/Spells/" + i + ".spell"))
                {
                    Globals.GameSpells[i].Save(i);
                }
                else
                {
                    Globals.GameSpells[i].Load(File.ReadAllBytes("Resources/Spells/" + i + ".spell"), i);
                }
            }
        }

        //Animations
        public static void LoadAnimations()
        {
            if (!Directory.Exists("Resources/Animations"))
            {
                Directory.CreateDirectory("Resources/Animations");
            }

            Globals.GameAnimations = new AnimationStruct[Constants.MaxAnimations];
            for (var i = 0; i < Constants.MaxAnimations; i++)
            {
                Globals.GameAnimations[i] = new AnimationStruct();
                if (!File.Exists("Resources/Animations/" + i + ".anim"))
                {
                    Globals.GameAnimations[i].Save(i);
                }
                else
                {
                    Globals.GameAnimations[i].Load(File.ReadAllBytes("Resources/Animations/" + i + ".anim"), i);
                }
            }
        }

        // Resources
        public static void LoadResources()
        {
            if (!Directory.Exists("Resources/Resources"))
            {
                Directory.CreateDirectory("Resources/Resources");
            }
            Globals.GameResources = new ResourceStruct[Constants.MaxResources];
            for (var i = 0; i < Constants.MaxResources; i++)
            {
                Globals.GameResources[i] = new ResourceStruct();
                if (!File.Exists("Resources/Resources/" + i + ".res"))
                {
                    Globals.GameResources[i].Save(i);
                }
                else
                {
                    Globals.GameResources[i].Load(File.ReadAllBytes("Resources/Resources/" + i + ".res"), i);
                }

            }
        }

        // Quests
        public static void LoadQuests()
        {
            if (!Directory.Exists("Resources/Quests"))
            {
                Directory.CreateDirectory("Resources/Quests");
            }
            Globals.GameQuests = new QuestStruct[Constants.MaxQuests];
            for (var i = 0; i < Constants.MaxQuests; i++)
            {
                Globals.GameQuests[i] = new QuestStruct();
                if (!File.Exists("Resources/Quests/" + i + ".qst"))
                {
                    Globals.GameQuests[i].Save(i);
                }
                else
                {
                    Globals.GameQuests[i].Load(File.ReadAllBytes("Resources/Quests/" + i + ".qst"), i);
                }

            }
        }

        // Projectiles
        public static void LoadProjectiles()
        {
            if (!Directory.Exists("Resources/Projectiles"))
            {
                Directory.CreateDirectory("Resources/Projectiles");
            }
            Globals.GameProjectiles = new ProjectileStruct[Constants.MaxProjectiles];
            for (var i = 0; i < Constants.MaxProjectiles; i++)
            {
                Globals.GameProjectiles[i] = new ProjectileStruct();
                if (!File.Exists("Resources/Projectiles/" + i + ".prj"))
                {
                    Globals.GameProjectiles[i].Save(i);
                }
                else
                {
                    Globals.GameProjectiles[i].Load(File.ReadAllBytes("Resources/Projectiles/" + i + ".prj"), i);
                }

            }
        }

        // Classes
        public static int LoadClasses()
        {
            int x = 0;
            if (!Directory.Exists("Resources/Classes"))
            {
                Directory.CreateDirectory("Resources/Classes");
            }
            Globals.GameClasses = new ClassStruct[Constants.MaxClasses];
            for (var i = 0; i < Constants.MaxClasses; i++)
            {
                Globals.GameClasses[i] = new ClassStruct();
                if (!File.Exists("Resources/Classes/" + i + ".cls"))
                {
                    Globals.GameClasses[i].Save(i);
                }
                else
                {
                    Globals.GameClasses[i].Load(File.ReadAllBytes("Resources/Classes/" + i + ".cls"),i);
                }
                if (String.IsNullOrEmpty(Globals.GameClasses[i].Name)) { x++; }
            }
            return x;
        }
        public static void CreateDefaultClass()
        {
            Globals.GameClasses[0].Name = "Default";
            ClassSprite defaultMale = new ClassSprite();
            defaultMale.Sprite = "1.png";
            defaultMale.Gender = 0;
            ClassSprite defaultFemale = new ClassSprite();
            defaultFemale.Sprite = "2.png";
            defaultFemale.Gender = 1;
            Globals.GameClasses[0].Sprites.Add(defaultMale);
            Globals.GameClasses[0].Sprites.Add(defaultFemale);
            for (int i = 0; i < (int)Enums.Vitals.VitalCount; i++)
            {
                Globals.GameClasses[0].MaxVital[i] = 20;
            }
            for (int i = 0; i < (int)Enums.Stats.StatCount; i++)
            {
                Globals.GameClasses[0].Stat[i] = 20;
            }
            Globals.GameClasses[0].Save(0);
        }

        //Map Folders
        private static void LoadMapFolders()
        {
            if (File.Exists("Resources/Maps/MapStructure.dat"))
            {
                ByteBuffer myBuffer = new ByteBuffer();
                myBuffer.WriteBytes(File.ReadAllBytes("Resources/Maps/MapStructure.dat"));
                MapStructure.Load(myBuffer);
                for (int i = 0; i < Globals.GameMaps.Length; i++)
                {
                    if (MapHelper.IsMapValid(i))
                    {
                        if (MapStructure.FindMap(i) == null)
                        {
                            MapStructure.AddMap(i);
                        }
                    }
                }
                File.WriteAllBytes("Resources/Maps/MapStructure.dat", MapStructure.Data());
                PacketSender.SendMapListToEditors();
            }
            else
            {
                for (int i = 0; i < Globals.GameMaps.Length; i++)
                {
                    if (MapHelper.IsMapValid(i))
                    {
                        MapStructure.AddMap(i);
                    }
                }
                File.WriteAllBytes("Resources/Maps/MapStructure.dat", MapStructure.Data());
            }
        }

        //Common Events
        public static void LoadCommonEvents()
        {
            if (!Directory.Exists("Resources/Common Events"))
            {
                Directory.CreateDirectory("Resources/Common Events");
            }
            Globals.CommonEvents = new EventStruct[Constants.MaxCommonEvents];
            for (var i = 0; i < Constants.MaxCommonEvents; i++)
            {
                Globals.CommonEvents[i] = new EventStruct(i,-1,-1,true);
                if (!File.Exists("Resources/Common Events/" + i + ".evt"))
                {
                    File.WriteAllBytes("Resources/Common Events/" + i + ".evt",Globals.CommonEvents[i].EventData());
                }
                else
                {
                    ByteBuffer bf = new ByteBuffer();
                    bf.WriteBytes(File.ReadAllBytes("Resources/Common Events/" + i + ".evt"));
                    Globals.CommonEvents[i] = new EventStruct(i, bf, true);
                }

            }
        }

        //Switches and Variables
        public static void LoadSwitchesAndVariables()
        {
            //Gonna do xml
            Globals.ServerSwitches = new string[Constants.MaxServerSwitches];
            Globals.ServerVariables = new string[Constants.MaxServerVariables];
            Globals.ServerSwitchValues = new bool[Constants.MaxServerSwitches];
            Globals.ServerVariableValues = new int[Constants.MaxServerVariables];
            Globals.PlayerSwitches = new string[Constants.MaxPlayerSwitches];
            Globals.PlayerVariables = new string[Constants.MaxPlayerVariables];
            LoadSwitchesOrVariabes(Globals.ServerSwitches, Globals.ServerSwitchValues, null, "GlobalSwitch", "ServerSwitches", Constants.MaxServerSwitches);
            LoadSwitchesOrVariabes(Globals.PlayerSwitches, null, null, "Switch", "PlayerSwitches", Constants.MaxPlayerSwitches);
            LoadSwitchesOrVariabes(Globals.ServerVariables, null, Globals.ServerVariableValues, "GlobalVariable", "ServerVariables", Constants.MaxServerVariables);
            LoadSwitchesOrVariabes(Globals.PlayerVariables, null, null, "Variable", "PlayerVariables", Constants.MaxPlayerVariables);
        }

        public static void SaveSwitchesAndVariables()
        {
            SaveSwitchesOrVariables(Globals.ServerSwitches, Globals.ServerSwitchValues, null, "GlobalSwitch", "ServerSwitches", Constants.MaxServerSwitches);
            SaveSwitchesOrVariables(Globals.PlayerSwitches, null, null, "Switch", "PlayerSwitches", Constants.MaxPlayerSwitches);
            SaveSwitchesOrVariables(Globals.ServerVariables, null, Globals.ServerVariableValues, "GlobalVariable", "ServerVariables", Constants.MaxServerVariables);
            SaveSwitchesOrVariables(Globals.PlayerVariables, null, null, "Variable", "PlayerVariables", Constants.MaxPlayerVariables);
        }
        private static void LoadSwitchesOrVariabes(string[] names, bool[] boolValues, int[] intValues, string prefix, string header, int count)
        {
            for (int i = 0; i < count; i++)
            {
                names[i] = prefix;
            }
            if (File.Exists("Resources/" + header + ".xml"))
            {
                try
                {
                    var xml = new XmlDocument();
                    xml.Load("resources\\" + header + ".xml");
                    for (int i = 0; i < Constants.MaxServerSwitches; i++)
                    {
                        
                        names[i] = xml.SelectSingleNode("//" + header + "/" + prefix + (i + 1) + "Name").InnerText;
                        if (boolValues != null)
                        {
                            boolValues[i] = Convert.ToBoolean( xml.SelectSingleNode("//" + header + "/" + prefix + (i + 1) + "Value").InnerText);
                        }
                    }
                }
                catch (Exception ex)
                {

                }
            }
            else
            {
                SaveSwitchesOrVariables(names, boolValues,intValues, prefix, header, count);
            }
        }
        public static void SaveSwitchesOrVariables(string[] names, bool[] boolValues, int[] intValues, string prefix, string header, int count)
        {
            var xml = new XmlWriterSettings { Indent = true };
            xml.ConformanceLevel = ConformanceLevel.Auto;
            var writer = XmlWriter.Create("resources\\" + header + ".xml", xml);

            writer.WriteStartDocument();
            writer.WriteStartElement(header);
            for (int i = 0; i < count; i++)
            {
                writer.WriteElementString(prefix + (i + 1) + "Name", names[i]);
                if (boolValues != null) writer.WriteElementString(prefix + (i + 1) + "Value", boolValues[i].ToString());
                if (intValues != null) writer.WriteElementString(prefix + (i + 1) + "Value", intValues[i].ToString());
            }
            writer.WriteEndElement();
            writer.WriteEndDocument();
            writer.Flush();
            writer.Close();
        }
    }
}

