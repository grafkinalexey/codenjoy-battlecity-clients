using System;
using CodeBattleNetLibrary;

namespace CodeBattleNet
{
    internal static class Program
    {
        private static void Main()
        {
            var gcb = new GameClientBattlecity("http://dojorena.io/codenjoy-contest/board/player/1kuzbp06snmmqz5tvzu6?code=1235345158290083479&gameName=battlecity");
            gcb.Run(() =>
            {
                Move(gcb);
            });
            Console.Read();
        }

        private static void Move(GameClientBattlecity gcb)
        {

            if(!gcb.Go())
            {
                RandomMove(gcb);
            }
        }

        private static void RandomMove(GameClientBattlecity gcb)
        {
            var r = new Random();
            var done = false;

            switch (r.Next(5))
            {
                case 0:
                    if (!gcb.IsBarrierAt(gcb.PlayerX, gcb.PlayerY - 1))
                    {
                        gcb.SendActions(gcb.Up() + "," + gcb.Act());
                        done = true;
                    }
                    break;
                case 1:
                    if (!gcb.IsBarrierAt(gcb.PlayerX + 1, gcb.PlayerY))
                    {
                        gcb.SendActions(gcb.Right() + "," + gcb.Act());
                        done = true;
                    }
                    break;
                case 2:
                    if (!gcb.IsBarrierAt(gcb.PlayerX, gcb.PlayerY + 1))
                    {
                        gcb.SendActions(gcb.Down() + "," + gcb.Act());
                        done = true;
                    }
                    break;
                case 3:
                    if (!gcb.IsBarrierAt(gcb.PlayerX - 1, gcb.PlayerY))
                    {
                        gcb.SendActions(gcb.Left() + "," + gcb.Act());
                        done = true;
                    }
                    break;
                case 4:
                    gcb.SendActions(gcb.Act());
                    done = true;
                    break;
            }
            if (done == false)
                gcb.SendActions(gcb.Blank());
        }
    }
}
