using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using HamsterCheese.AmongUsMemory;

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

    private static string _name;

    private static string _context;

    private static int _direction;

    private static PlayerData _localPlayer;

    private const int UpdateTime = 1 / 150 * 1000;

    private static void UpdateCheat()
    {
        try
        {
                
                
            Console.WriteLine("Initializing mumble");
            init_mumble();
            Console.WriteLine("finished initializing mumble");
            bool saidWait = false;
            while (true)
            {
                update_mumble(_x, _y, _z, _direction, _name, _context);
                Thread.Sleep(UpdateTime);

                var status = Cheese.GetShipStatus();

                if (status.OwnerId == 0 && !saidWait)
                {
                    Console.WriteLine("Not in game, waiting....");
                    _localPlayer = null;
                    saidWait = true;
                    continue;
                }

                if (status.OwnerId != 0 && saidWait) saidWait = false;

                if (_localPlayer == null)
                {
                    _localPlayer = _playerDataList.Find(s => s.IsLocalPlayer);
                    continue;
                }

                if (!_localPlayer.PlayerInfo.HasValue)
                {
                    _localPlayer = null;
                    continue;
                }
                    
                var playerName = Utils.ReadString(_localPlayer.PlayerInfo.Value.PlayerName);
                    
                int dir = _lastX > _localPlayer.Position.x ? 0 : 1;

                if (_dead && _localPlayer.PlayerInfo.Value.IsDead == 0)
                {
                    _dead = false;
                }

                if (_localPlayer.PlayerInfo.Value.IsDead == 1)
                {
                    Task.Delay(5000).ContinueWith(t => _dead = true);
                    Console.WriteLine("Died, 5 sec until no more positional audio");
                }


                _x = _dead ? 0 : _localPlayer.Position.x;
                _y = 0;
                _z = _dead ? 0 : _localPlayer.Position.y;

                _y = 0;
                _name = playerName;
                _direction = dir;
                _context = "main";
                _lastX = _localPlayer.Position.x;

                Console.WriteLine(
                    $"Name : {playerName}, x : {_localPlayer.Position.x} , y: {_localPlayer.Position.y}, dead: {_dead}, direction: " +
                    (dir == 0 ? "left" : "right"));
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.ToString());
        }
    }

    private static bool _dead;


    private static void Main()
    {
        try
        {
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
                    foreach (var player in _playerDataList)
                    {
                        player.StopObserveState();
                    }


                    _playerDataList = Cheese.GetAllPlayers();


                    foreach (var player in _playerDataList)
                    {
                        if (player.IsLocalPlayer)
                        {
                            player.StartObserveState();
                        }
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
}