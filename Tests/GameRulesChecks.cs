using System;
using PotionPanic;

static class GameRulesChecks
{
    static int checks;
    static void Check(bool ok, string message) { if (!ok) throw new Exception(message); checks++; }
    static void Measure(PotionGame g, Order o)
    {
        g.Take(o);
        for (int i = 0; i < 3; i++) g.Pour(o, i, PotionGame.Targets[o.Recipe, i] / 22f);
        g.Prepare(o);
    }
    static void Finish(PotionGame g, Order o)
    {
        g.Stir(o, 2.8f); g.FinishStir(o);
        o.ChosenBottle = o.Bottle; o.ChosenGarnish = o.Garnish;
        Check(!g.Serve(o, o.Number + 1), "Wrong customer must not receive potion");
        Check(g.Serve(o, o.Number), "Correct ticket must serve");
        Check(!g.Serve(o, o.Number), "Cannot serve twice");
    }
    static void Main()
    {
        var practice = new PotionGame(true); var p = practice.Orders[0];
        Measure(practice, p); Check(p.IngredientsRating > 99.99f, "Exact recipe score");
        practice.StartBrew(p); practice.Tick(9); practice.StopBrew(p); Finish(practice, p);
        Check(p.Overall > 99.99f && practice.Complete && practice.Earnings == 25, "Perfect practice and payment");
        practice.Tick(200); Check(practice.Orders.Count == 1, "Practice has one customer");
        var g = new PotionGame(false);
        for (int i = 0; i < 4; i++) g.Tick(22);
        Check(g.Orders.Count == 5, "Five arrivals");
        for (int i = 0; i < 3; i++) Measure(g, g.Orders[i]);
        Check(!g.Take(g.Orders[3]), "Three active tickets maximum");
        Check(g.StartBrew(g.Orders[0]) && g.StartBrew(g.Orders[1]), "Two simultaneous brews");
        Check(!g.StartBrew(g.Orders[2]), "Third cauldron blocked");
        g.Tick(9);
        Check(g.Orders[0].BrewTime == 9 && g.Orders[1].BrewTime == 9, "Both advance in background");
        g.StopBrew(g.Orders[0]);
        Check(!g.StartBrew(g.Orders[2]), "Stirring occupies slot");
        Finish(g, g.Orders[0]);
        Check(g.StartBrew(g.Orders[2]), "Finishing frees cauldron");
        g.Tick(7);
        Check(g.Orders[1].Phase == Phase.Stirring && g.Orders[1].BrewRating == 0, "Overheating recovers to stirring");
        Finish(g, g.Orders[1]);
        g.Tick(2); g.StopBrew(g.Orders[2]); Finish(g, g.Orders[2]);
        for (int i = 3; i < 5; i++)
        { var o = g.Orders[i]; Measure(g,o); g.StartBrew(o); g.Tick(9); g.StopBrew(o); Finish(g,o); }
        Check(g.Complete && g.Served == 5 && g.Earnings > 0, "Full day completes");
        var mistakes = new PotionGame(false, 600); var m = mistakes.Orders[0]; mistakes.Take(m);
        mistakes.Pour(m, 2, 20); Check(m.Amounts[2] == 100, "Pour is capped");
        mistakes.Clear(m); Check(m.Amounts[2] == 0, "Empty mixture resets");
        mistakes.Pour(m, 2, 4); mistakes.Prepare(m); Check(m.IngredientsRating == 0, "Wrong ingredients penalized");
        mistakes.StartBrew(m); mistakes.StopBrew(m); mistakes.Stir(m, 10); mistakes.FinishStir(m);
        Check(!mistakes.Serve(m, m.Number), "Finishes required");
        m.ChosenBottle = 1; m.ChosenGarnish = 1; mistakes.Tick(400); mistakes.Serve(m,m.Number);
        Check(m.FinishRating == 0 && m.WaitRating == 0 && m.Payment >= 5, "Mistakes lower scores but allow serving before closing");
        var deadline = new PotionGame(false, 30, 8);
        var last = deadline.Orders[0]; Measure(deadline,last); deadline.StartBrew(last);
        deadline.Tick(9); deadline.StopBrew(last); deadline.Stir(last,2.8f); deadline.FinishStir(last);
        last.ChosenBottle=last.Bottle; last.ChosenGarnish=last.Garnish;
        deadline.Tick(20.9f); Check(!deadline.Complete, "Shift stays playable just before closing");
        deadline.Tick(1); Check(deadline.Complete && deadline.Failed && !deadline.Won && deadline.Remaining==0, "Closing time is a real loss");
        Check(!deadline.Serve(last,last.Number), "Cannot earn coins after losing");
        Check(!deadline.Take(deadline.Orders[1]), "Cannot take orders after losing");
        float stopped=deadline.Elapsed; deadline.Tick(100); Check(deadline.Elapsed==stopped, "Ended shift stays frozen");
        Check(g.Won && !g.Failed, "Serving all five before closing wins");
        var tutorial=new PotionGame(true,30); tutorial.Tick(1000);
        Check(!tutorial.Failed && !tutorial.Complete, "Practice has no closing-time failure");
        var rush=new PotionGame(false,135,8); rush.Tick(32);
        Check(rush.DayLength==135 && rush.Orders.Count==5, "Mode settings change deadline and arrivals");
        Console.WriteLine("PASS: " + checks + " gameplay checks");
    }
}
