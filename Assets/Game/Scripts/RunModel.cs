using System;

namespace AfterHours
{
    public enum GameState { Menu, Starting, Playing, Paused, GameOver }

    // Distance is a high-water mark: revisiting road never awards distance points twice.
    public sealed class RunModel
    {
        public int Lives { get; private set; } = 3;
        public float Fuel { get; private set; } = 100;
        public double Furthest { get; private set; }
        public int Coins { get; private set; }
        public int Score => (int)(Furthest * 2) + Coins * 25;
        public float Difficulty => Math.Min((float)Furthest / 6000f, 1f);
        public float CruiseSpeed => 17f + Difficulty * 17f;
        public void Reset() { Lives = 3; Fuel = 100; Furthest = 0; Coins = 0; }
        public void Advance(double position, float seconds)
        {
            Furthest = Math.Max(Furthest, Math.Max(0, position));
            Fuel = Math.Max(0, Fuel - seconds * .7f);
        }
        public void Hit() { Lives = Math.Max(0, Lives - 1); }
        public void AddCoin() { Coins++; }
        public void Refuel() { Fuel = Math.Min(100, Fuel + 30); }
    }
}
