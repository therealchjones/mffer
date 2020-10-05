using System.Collections.Generic;

namespace MFFDataApp
{
    public class Player : Component
    {
        public Alliance alliance { get; set; }
        public List<MyCharacter> MyRoster { get; set; }
    }
    public class MyCharacter
    {
        public Character BaseCharacter { get; set; }
    }
}