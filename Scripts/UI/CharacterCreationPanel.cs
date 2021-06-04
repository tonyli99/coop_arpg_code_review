namespace Game
{
    using UnityEngine;
    using UnityEngine.UI;
    using TMPro;

    /// <summary>
    /// The UI elements that make up the character creation panel, which a
    /// ControllerPlayer shows when it doesn't yet have a character.
    /// </summary>
    public class CharacterCreationPanel : MonoBehaviour
    {

        [SerializeField] private Image bodyImage;
        [SerializeField] private Image eyesImage;
        [SerializeField] private Image outfitImage;
        [SerializeField] private Image hairImage;

        [Space]

        [SerializeField] private TextMeshProUGUI categoryText;
        [SerializeField] private Button previousStyleButton;
        [SerializeField] private Button nextStyleButton;
        [SerializeField] private Button acceptButton;

        [Space]

        [SerializeField] private CharacterParts characterParts;

        public Image BodyImage { get { return bodyImage; } }
        public Image EyesImage { get { return eyesImage; } }
        public Image OutfitImage { get { return outfitImage; } }
        public Image HairImage { get { return hairImage; } }

        public TextMeshProUGUI CategoryText { get { return categoryText; } }
        public Button PreviousStyleButton { get { return previousStyleButton; } }
        public Button NextStyleButton { get { return nextStyleButton; } }        
        public Button AcceptButton { get { return acceptButton; } }

        public CharacterParts CharacterParts { get { return characterParts; } }

        public void Randomize()
        {
            bodyImage.sprite = characterParts.Bodies[Random.Range(0, characterParts.NumMaleBodies)];
            eyesImage.sprite = characterParts.MaleEyes[Random.Range(0, characterParts.MaleEyes.Count)];
            hairImage.sprite = characterParts.MaleHair[Random.Range(0, characterParts.MaleHair.Count)];
            outfitImage.sprite = characterParts.MaleOutfits[Random.Range(0, characterParts.MaleOutfits.Count)];
            bodyImage.enabled = (BodyImage.sprite != null);
            eyesImage.enabled = (eyesImage.sprite != null);
            hairImage.enabled = (hairImage.sprite != null);
            hairImage.color = characterParts.HairColors[0];
        }

    }
}
