namespace Game
{
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// Asset file containing character customization images that
    /// a ControllerPlayer can cycle through when creating a
    /// character. The indices of these lists should match the
    /// indices in CharacterPartPrefabs, except since the body
    /// images are a combined male + female list, numMaleBodies
    /// specifies how many at the front of the list are male bodies.
    /// </summary>
    [CreateAssetMenu]
    public class CharacterParts : ScriptableObject
    {

        [SerializeField] private List<Sprite> bodies;
        [SerializeField] private int numMaleBodies;

        [Header("Assets - Male")]
        [SerializeField] private List<Sprite> maleEyes;
        [SerializeField] private List<Sprite> maleHair;
        [SerializeField] private List<Sprite> maleOutfits;

        [Header("Assets - Female")]
        [SerializeField] private List<Sprite> femaleEyes;
        [SerializeField] private List<Sprite> femaleHair;
        [SerializeField] private List<Sprite> femaleOutfits;

        [Header("Colors")]
        [SerializeField] private List<Color> hairColors;

        public List<Sprite> Bodies { get { return bodies; } }

        public int NumMaleBodies { get { return numMaleBodies; } }

        public List<Sprite> MaleEyes { get { return maleEyes; } }
        public List<Sprite> MaleHair { get { return maleHair; } }
        public List<Sprite> MaleOutfits { get { return maleOutfits; } }

        public List<Sprite> FemaleEyes { get { return femaleEyes; } }
        public List<Sprite> FemaleHair { get { return femaleHair; } }
        public List<Sprite> FemaleOutfits { get { return femaleOutfits; } }

        public List<Color> HairColors { get { return hairColors; } }

        public bool IsMale(int bodyIndex)
        {
            return bodyIndex < numMaleBodies;
        }

        public string GetClassName(int index)
        {
            if (index == 0) return "Cleric";
            if (index == 1) return "Warrior";
            if (index == 2) return "Wizard";
            return "Class";
        }

    }
}
