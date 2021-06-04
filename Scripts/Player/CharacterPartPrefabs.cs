namespace Game
{
    using UnityEngine;

    /// <summary>
    /// Asset file pointing to gendered character part prefabs. There will 
    /// be one asset file for male prefabs and another for female prefabs. 
    /// A character will be composed of zero or one of each prefab category.
    /// The Character class will receive an array index for each part and will
    /// instantiate the corresponding prefab listed in this asset.
    /// </summary>
    [CreateAssetMenu]
    public class CharacterPartPrefabs : ScriptableObject
    {

        [SerializeField] private GameObject[] bodies;
        [SerializeField] private GameObject[] eyes;
        [SerializeField] private GameObject[] hair;
        [SerializeField] private GameObject[] outfits;

        public GameObject[] Bodies { get { return bodies; } }
        public GameObject[] Eyes { get { return eyes; } }
        public GameObject[] Hair { get { return hair; } }
        public GameObject[] Outfits { get { return outfits; } }

    }
}
