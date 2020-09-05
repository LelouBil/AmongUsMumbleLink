using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using AutoUpdaterDotNET;
using HamsterCheese.AmongUsMemory;
using Newtonsoft.Json;

internal static class Program
{
    [DllImport("MumbleLinkDLL.dll")]
    private static extern void init_mumble();

    [DllImport("MumbleLinkDLL.dll")]
    private static extern void update_mumble(float x, float y, float z, int dir, string name, string context);

    private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        // Log the exception, display it, etc
        Console.WriteLine((e.ExceptionObject as Exception)?.Message);
    }

    private static List<PlayerData> _playerDataList = new List<PlayerData>();

    private static float _lastX;

    private static float _x;

    private static float _y;

    private static float _z;

    private static string _name = "user";

    private static string _context = "dead";

    private static int _direction;

    private static PlayerData _localPlayer;
    private static bool _saidWait;

    private const int UpdateTime = 1 / 150 * 1000;

    private static void UpdateCheat()
    {
        _saidWait = false;
        try
        {
                
                
            Console.WriteLine("Initializing mumble");
            init_mumble();
            Console.WriteLine("finished initializing mumble");
            int dir = 0;
            
            ShipStatus status = Cheese.GetShipStatus();
                        
            if (status.OwnerId == 0 && !_saidWait)
            {
                foreach (var player in _playerDataList)
                {
                    player.StopObserveState();
                }
                Console.WriteLine("Not in game, waiting....");
                _x = 0;
                _y = 0;
                _z = 0;
                _localPlayer = null;
                _saidWait = true;
            }
            else
            {

                _playerDataList = Cheese.GetAllPlayers();

                foreach (var player in _playerDataList)
                {
                    if (player.IsLocalPlayer)
                    {
                        player.StartObserveState();
                    }
                }
            }

            while (true)
            {
                update_mumble(_x, _y, _z, _direction, _name, _context);
                Thread.Sleep(UpdateTime);
                //Console.WriteLine("hey");
                
                _localPlayer = _playerDataList.Find(s => s.IsLocalPlayer);

                if (_localPlayer == null)
                {
                    _x = 0;
                    _y = 0;
                    _z = 0;
                    Console.WriteLine("Not in game, waiting....");
                    _localPlayer = null;
                    _saidWait = true;
                    continue;
                }

                if (!_localPlayer.PlayerInfo.HasValue)
                {
                    _x = 0;
                    _y = 0;
                    _z = 0;
                    _localPlayer = null;
                    Console.WriteLine("Not in game, waiting....");
                    _x = 0;
                    _y = 0;
                    _z = 0;
                    _localPlayer = null;
                    _saidWait = true;
                    continue;
                }
                
                _name = Utils.ReadString(_localPlayer.PlayerInfo.Value.PlayerName);


                if (_lastX > _localPlayer.Position.x)
                    dir = 0;
                else if (_lastX < _localPlayer.Position.x)
                {
                    dir = 1;
                }


                _x = _localPlayer.Position.x;
                _y = 0;
                _z = _localPlayer.Position.y;

                
                _direction = dir;
                _context = "main";
                _lastX = _localPlayer.Position.x;

                Console.WriteLine(
                    $"Name : {_name}, x : {_localPlayer.Position.x} , y: {_localPlayer.Position.y},  direction: " +
                    (dir == 0 ? "left" : "right") +$" , context: {_context}");
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.ToString());
        }
    }

    private static void Main()
    {
        try
        {
            AutoUpdater.HttpUserAgent = "AutoUpdater";
            AutoUpdater.ParseUpdateInfoEvent += OnParseUpdateInfo;
            AutoUpdater.Start("https://api.github.com/repos/LelouBil/AmongUsMumbleLink/releases/latest");
            Console.WriteLine("Starting");
            AppDomain.CurrentDomain.UnhandledException +=
                CurrentDomain_UnhandledException;

            // Cheat Init
            Console.WriteLine("try init cheese");
            if (Cheese.Init())
            {
                Console.WriteLine("Initialized cheese");
                // Update Player Data When Every Game
                Cheese.ObserveShipStatus((shipStat) =>
                {
                    
                    try
                    {
                        ShipStatus status = Cheese.GetShipStatus();
                        
                        if (status.OwnerId == 0 && !_saidWait)
                        {
                            foreach (var player in _playerDataList)
                            {
                                player.StopObserveState();
                            }
                            Console.WriteLine("Not in game, waiting....");
                            _x = 0;
                            _y = 0;
                            _z = 0;
                            _localPlayer = null;
                            _playerDataList = new List<PlayerData>();
                            _saidWait = true;
                            return;
                        }

                        if (status.OwnerId != 0 && _saidWait) _saidWait = false;
                        
                        _playerDataList = Cheese.GetAllPlayers();

                        foreach (var player in _playerDataList)
                        {
                            if (player.IsLocalPlayer)
                            {
                                player.StartObserveState();
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        if (!_saidWait)
                        {
                            foreach (var player in _playerDataList)
                            {
                                player.StopObserveState();
                            }
                            Console.WriteLine("Not in game, waiting....");
                            _localPlayer = null;
                            _saidWait = true;
                            _x = 0;
                            _y = 0;
                            _z = 0;
                        }

                        return;
                    }
                    
                });
                Console.WriteLine("Starting Thread");
                CancellationTokenSource cts = new CancellationTokenSource();
                Task.Factory.StartNew(
                    UpdateCheat, cts.Token).Wait(cts.Token);
            }
            else
            {
                Console.WriteLine("Failed init cheese, game is not running or app is not running as admin");
                
            }

        }
        catch (Exception e)
        {
            Console.WriteLine(e.ToString());
        }
        Console.WriteLine("Press any key to stop...");
        Console.ReadKey();
    }

    private static void OnParseUpdateInfo(ParseUpdateInfoEventArgs args)
    {
        dynamic json = JsonConvert.DeserializeObject(args.RemoteData);
        if (json != null)
            args.UpdateInfo = new UpdateInfoEventArgs
            {
                CurrentVersion = json.tag_name,
                ChangelogURL = json.html_url,
                DownloadURL = json.assets[0].browser_download_url,
                Mandatory = new Mandatory
                {
                    Value = true,
                    UpdateMode = Mode.Forced,
                }
            };
    }
}