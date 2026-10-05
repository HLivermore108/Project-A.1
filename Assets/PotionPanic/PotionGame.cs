using System;
using System.Collections.Generic;

namespace PotionPanic
{
    // These are the steps one order goes through, from joining the line to being served.
    public enum Phase { Waiting, Measuring, ReadyToBrew, Brewing, Stirring, Finishing, Served }

    // Keep everything for a customer together so switching stations doesn't lose their potion.
    public sealed class Order
    {
        public int Number, Recipe, Bottle, Garnish;
        public string Customer;
        public Phase Phase;
        public float Wait, BrewTime, StirAmount;
        // The ingredient order is always berries, flowers, then leaves. Amounts are in ml.
        public readonly float[] Amounts = new float[3];
        // -1 means the player hasn't picked a bottle or garnish yet. 0 is a real choice.
        public int ChosenBottle = -1, ChosenGarnish = -1;
        public float IngredientsRating, BrewRating, FinishRating, WaitRating;
        public int Payment, Tip;
        // Each part counts equally toward the customer's final rating.
        public float Overall => (IngredientsRating + BrewRating + FinishRating + WaitRating) / 4f;
    }

    // This file handles the game rules. Drawing the shop and reading clicks happen in PotionPanicApp.
    public sealed class PotionGame
    {
        public static readonly string[] Ingredients = { "Ruby berries", "Moon flowers", "Sage leaves" };
        public static readonly string[] Recipes = { "Heartglow", "Moonwake", "Wildwhisper" };
        public static readonly string[] Bottles = { "Round flask", "Tall vial" };
        public static readonly string[] Garnishes = { "Stardust", "Sage sprig" };
        // Each row is a recipe; each column is an ingredient. A zero means leave that ingredient out.
        public static readonly int[,] Targets = { { 60, 40, 0 }, { 0, 55, 45 }, { 35, 0, 65 } };
        // Timing values are in seconds. The player gets a two-second window for a perfect brew.
        public const float BrewStart = 8f, BrewEnd = 10f, BurnTime = 15f;
        public readonly List<Order> Orders = new List<Order>();
        public bool Practice { get; private set; }
        public float Elapsed { get; private set; }
        public int Earnings { get; private set; }
        public int Served { get; private set; }
        // A normal day has a real deadline. Practice stays untimed while learning the controls.
        public float DayLength { get; private set; }
        public float ArrivalInterval { get; private set; }
        public float Remaining => Math.Max(0, DayLength - Elapsed);
        public bool Won => Served == (Practice ? 1 : 5);
        public bool Failed => !Practice && !Won && Elapsed >= DayLength;
        public bool Complete => Won || Failed;
        // Waiting customers don't use a ticket slot until their order is taken.
        public int ActiveCount => Orders.FindAll(o => o.Phase != Phase.Waiting && o.Phase != Phase.Served).Count;
        // Turning off the heat doesn't free a cauldron: the potion still needs stirring.
        public int BrewingCount => Orders.FindAll(o => o.Phase == Phase.Brewing || o.Phase == Phase.Stirring).Count;
        public int TotalTips { get; private set; }
        public PotionGame(bool practice, float dayLength = 240f, float arrivalInterval = 22f)
        {
            Practice = practice;
            DayLength = Math.Max(30, dayLength);
            ArrivalInterval = Math.Max(1, arrivalInterval);
            Arrive();
        }

        // The first day uses a set customer order, so replaying gives the same challenge.
        void Arrive()
        {
            int i = Orders.Count;
            string[] names = { "Mabel the Witch", "Sir Bramble", "Pip the Goblin", "Luna the Witch", "Captain Moss" };
            Orders.Add(new Order { Number = i + 1, Customer = names[i], Recipe = i % 3,
                Bottle = i % 2, Garnish = (i / 2) % 2, Phase = Phase.Waiting });
        }

        // dt is the time since the last frame. Using seconds keeps timers independent of frame rate.
        public void Tick(float dt)
        {
            if (Complete || dt <= 0) return;
            if (!Practice) dt = Math.Min(dt, Remaining);
            Elapsed += dt;
            // Catch up on arrivals even if a slow frame crosses more than one arrival time.
            while (!Practice && Orders.Count < 5 && Elapsed >= Orders.Count * ArrivalInterval) Arrive();
            foreach (Order o in Orders)
            {
                // Waiting starts when the customer arrives, not when their ticket is taken.
                if (o.Phase != Phase.Served) o.Wait += dt;
                if (o.Phase == Phase.Brewing)
                {
                    o.BrewTime += dt;
                    // An overcooked potion loses points, but can still be finished and served.
                    if (o.BrewTime >= BurnTime) StopBrew(o);
                }
            }
        }

        public bool Take(Order o)
        {
            // Returning false lets the interface refuse an action without changing the order.
            if (Complete || !Orders.Contains(o) || o.Phase != Phase.Waiting || ActiveCount >= 3) return false;
            o.Phase = Phase.Measuring; return true;
        }
        public void Pour(Order o, int ingredient, float dt)
        {
            if (Complete || o == null || o.Phase != Phase.Measuring || ingredient < 0 || ingredient > 2 || dt <= 0) return;
            // Pour at 22 ml per second, stopping at the top of the 100 ml meter.
            o.Amounts[ingredient] = Math.Min(100, o.Amounts[ingredient] + dt * 22f);
        }
        // Ingredients are unlimited, so the player can empty a mixture and try again before brewing.
        public void Clear(Order o) { if (!Complete && o != null && o.Phase == Phase.Measuring) Array.Clear(o.Amounts, 0, 3); }
        public bool Prepare(Order o)
        {
            if (Complete || o == null || o.Phase != Phase.Measuring) return false;
            float error = 0;
            // Count both underpouring and overpouring, including ingredients the recipe didn't ask for.
            for (int i = 0; i < 3; i++) error += Math.Abs(o.Amounts[i] - Targets[o.Recipe, i]);
            o.IngredientsRating = Clamp(100 - error);
            o.Phase = Phase.ReadyToBrew; return true;
        }
        public bool StartBrew(Order o)
        {
            if (Complete || o == null || o.Phase != Phase.ReadyToBrew || BrewingCount >= 2) return false;
            o.Phase = Phase.Brewing; return true;
        }
        public bool StopBrew(Order o)
        {
            if (Complete || o == null || o.Phase != Phase.Brewing) return false;
            // Anywhere inside 8-10 seconds is perfect. Outside it, lose 20 points per second.
            float distance = o.BrewTime < BrewStart ? BrewStart - o.BrewTime : Math.Max(0, o.BrewTime - BrewEnd);
            o.BrewRating = Clamp(100 - distance * 20);
            o.Phase = Phase.Stirring; return true;
        }
        public void Stir(Order o, float dt)
        {
            if (!Complete && o != null && o.Phase == Phase.Stirring && dt > 0) o.StirAmount = Math.Min(100, o.StirAmount + dt * 25);
        }
        public bool FinishStir(Order o)
        {
            if (Complete || o == null || o.Phase != Phase.Stirring) return false;
            // Stirring past 70 also loses points, so holding the button forever isn't the best answer.
            float stirScore = Clamp(100 - Math.Abs(o.StirAmount - 70) * 3);
            // Heat timing and stirring each make up half of the brewing rating.
            o.BrewRating = (o.BrewRating + stirScore) / 2;
            o.Phase = Phase.Finishing; return true;
        }
        public bool Serve(Order o, int ticket)
        {
            // Check the ticket number before payment so a potion can't go to the wrong customer.
            if (Complete || o == null || o.Phase != Phase.Finishing || o.Number != ticket || o.ChosenBottle < 0 || o.ChosenGarnish < 0) return false;
            // Bottle and garnish are worth 50 points each.
            o.FinishRating = (o.ChosenBottle == o.Bottle ? 50 : 0) + (o.ChosenGarnish == o.Garnish ? 50 : 0);
            // Give customers 70 seconds of patience before deducting points. Practice has no penalty.
            o.WaitRating = Practice ? 100 : Clamp(100 - Math.Max(0, o.Wait - 70) * .55f);
            // Even a bad potion earns a little. Tips start above a 65% overall rating.
            o.Payment = 5 + (int)Math.Round(o.Overall * .15f);
            o.Tip = (int)Math.Round(Math.Max(0, o.Overall - 65) / 7);
            Earnings += o.Payment + o.Tip; TotalTips += o.Tip;
            o.Phase = Phase.Served; Served++; return true;
        }
        // Ratings should never go below 0 or above 100, even after a big mistake.
        public static float Clamp(float x) => Math.Max(0, Math.Min(100, x));
    }
}
