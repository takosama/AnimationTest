using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DxLibDLL;
using System.Threading;

namespace AnimationTest
{
    class Commands
    {
        public static Command[] CommandArray = new Command[]
            {
   
            new Command("ship","StageIn","0"),
            new Command("wait","","10"),
            new Command("ship","StageIn","1"),
            new Command("wait","","10"),
            new Command("ship","StageIn","2"),
            new Command("wait","","10"),
            new Command("ship","StageIn","3"),
            new Command("wait","","10"),
             new Command("ship","StageIn","4"),
            new Command("wait","","10"),
            new Command("ship","StageIn","5"),
            new Command("wait","","10"),
           
          
            };
    }
    class Command
    {
        public Command(string type, string func, string other)
        {
            this.Type = type;
            this.Func = func;
            this.Other = other;
        }
        public string Type;
        public string Func;
        public string Other;
    }

    class Program
    {
        static void Main(string[] args)
        {

            DX.ChangeWindowMode(1);
            DX.SetWindowSize(800, 480);
            DX.SetGraphMode(800, 480, 32);
            DX.SetDrawScreen(DX.DX_SCREEN_BACK);

            DX.DxLib_Init();


            var BackGroundImage = new BackGroundImage("Background.jpg");
            AnimationManager.SetAnimation(BackGroundImage);
            int flame = 0;
            int i = 0;
            var cmmand = Commands.CommandArray;
            CommandInterpriter.SetCommads(cmmand);
            while (true)
            {
                CommandInterpriter.Run(flame);
                AnimationManager.Refresh();
                //   Thread.Sleep(1000 / 60);
                DX.ProcessMessage();
                flame++;
            }

            DX.DxLib_End();
        }
    }

    class CommandInterpriter
    {
        static bool IsRunnning = false;
        static Queue<Command> queue = new Queue<Command>();
        public static void SetCommads(Command[] Commands)
        {
            IsRunnning = true;
            foreach (var item in Commands)
            {
                queue.Enqueue(item);
            }
        }
        static Command command = null;
        static int waitStart = 0;
        static int waitTime = -1;
        public static void Run(int flame)
        {
            if (command == null)
                goto fi;
          
            if (command.Type == "wait")
            {
                if (waitTime == -1)
                {
                    waitStart = flame;
                    waitTime = int.Parse(command.Other);
                }
                if (waitTime < (flame - waitStart))
                {
                    waitTime = -1;
                    command = null;
                    goto fi;
                }
            }
            if(command.Type=="ship")
            {
                if(command.Func=="StageIn")
                {
                    var line = int.Parse(command.Other);
                    var tmp = new ShipAnimation(0, line);
                    tmp.SetNextMode = ShipModeManager.Mode.StageIn;
                    AnimationManager.SetAnimation(tmp);
                }
              
                    command = null;
                    goto fi;
                
            }
            //
            fi:
            if (command == null)
            {
                if (queue.Count == 0)
                {
                    IsRunnning = false;
                    return;
                }
                command = queue.Dequeue();
                Run(flame);
            }
        }
    }

    class Animation : IDisposable
    {
        protected int Hdl;
        public int GHdl
        {
            get
            {
                return Hdl;
            }
        }
        protected (int x, int y) StartPos;
        protected (int x, int y) EndPos;
        public Func<int, (int x, int y)?> GetPosition;
        public void Dispose()
        {
            DX.DeleteGraph(Hdl);
        }
    }

    class BackGroundImage : Animation, IDisposable
    {
        public BackGroundImage(string FileName)
        {
            StartPos = (0, 0);
            EndPos = (0, 0);
            GetPosition = (int time) => (0, 0);
            Hdl = DX.LoadGraph(FileName);
        }
    }


    class ShipModeManager
    {
        public enum Mode
        {
            nop,
            stop,
            StageIn,
            notSet,
        }
        static public Mode GetNetMode(int time, Mode mode)
        {

            if (mode == Mode.StageIn)
            {
                if (time > 1000 / 60)
                    return Mode.stop;
            }
            return mode;
        }

    }



    class ShipAnimation : Animation
    {
        int Line = 0;
        int timeOffSet = 0;
        ShipModeManager.Mode mode;
        Func<int, (int x, int y)?> func;
        public ShipAnimation(int ID, int Line)
        {
            Hdl = DX.LoadGraph("ship.jpg");
            func = nop;
            this.Line = Line;
            GetPosition = _GetPosition;
            mode = ShipModeManager.Mode.notSet;
        }
        public ShipModeManager.Mode SetNextMode = ShipModeManager.Mode.notSet;


        public (int x, int y)? defalt = (0, 0);
        (int x, int y)? _GetPosition(int Time)
        {
            int time = Time - timeOffSet;
            ShipModeManager.Mode nextmode;
            if (SetNextMode == ShipModeManager.Mode.notSet)
                nextmode = ShipModeManager.GetNetMode(time, mode);
            else
            {
                nextmode = SetNextMode;
                SetNextMode = ShipModeManager.Mode.notSet;
            }
            if (mode != nextmode)
            {
                mode = nextmode;
                if (mode == ShipModeManager.Mode.StageIn)
                {
                    func = StageIn;
                }
                if (mode == ShipModeManager.Mode.stop)
                {
                    defalt = func(time);
                    if (defalt == null)
                        throw new Exception();
                    func = Stop;
                }
                timeOffSet = Time;

            }
            mode = nextmode;
            return func(time);
        }


        (int x, int y)? nop(int Time)
        {
            return (-100, -100);
        }

        (int x, int y)? StageIn(int Time)
        {

            //1000ms
            int x = -(int)(160.0 / (1000.0 / 60.0) * Time) + 800;
            int y = Line * 40 + 80;
            if (Time > 1000 / 60)
                x = 800 - 160;

            return (x, y);
        }

        (int x, int y)? Stop(int time)
        {
            return defalt;
        }
    }
    //test
    class CatImage : Animation, IDisposable
    {
        public CatImage(string FileName)
        {
            StartPos = (0, 0);
            EndPos = (800, 480);
            double TotalTime = 5000.0 / 60.0;
            GetPosition = (int time) =>
            {
                if (time > TotalTime)
                {
                    AnimationManager.SetAnimation(new CatImage("Cat.png"));
                    return null;
                }

                var x = 0;//(double)EndPos.x / TotalTime * time;
                var y = (double)EndPos.y / TotalTime * time;
                return ((int)x, (int)y);

            };
            Hdl = DX.LoadGraph(FileName);
        }
    }

    class AnimationManager
    {
        static int Count = 0;
        static List<(Animation animation, int setTime)> list = new List<(Animation animation, int setTime)>();
        static public void SetAnimation(Animation animation)
        {
            list.Add((animation, Count));
        }

        static public void Refresh()
        {
            DX.ClearDrawScreen();
            List<int> del = new List<int>();
            int cnt = -1;
            foreach (var item in list.ToArray())
            {
                cnt++;
                int time = Count - item.setTime;
                var tmp = item.animation.GetPosition(time);
                if (tmp == null)
                {
                    list.Remove(item);
                    continue;
                }
                var pos = tmp.Value;
                DX.DrawGraph(pos.x, pos.y, item.animation.GHdl, 1);
            }
            Count++;
            DX.ScreenFlip();
        }
    }
}
