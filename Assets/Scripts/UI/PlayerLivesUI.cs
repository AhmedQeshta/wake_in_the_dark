using UnityEngine;
using UnityEngine.UI;

public class PlayerLivesUI : MonoBehaviour
{
    // ==================================================
    // LIFE ICONS
    // ==================================================

    [Header("Life Icons")]
    [SerializeField]
    private Image[] lifeIcons;


    // ==================================================
    // UPDATE LIVES
    // ==================================================

    public void UpdateLives(int currentLives)
    {
        if (lifeIcons == null)
            return;


        for (int i = 0; i < lifeIcons.Length; i++)
        {
            if (lifeIcons[i] == null)
                continue;


            /*
             * If:
             *
             * currentLives = 3
             *
             * Life_01 = ON
             * Life_02 = ON
             * Life_03 = ON
             *
             *
             * currentLives = 2
             *
             * Life_01 = ON
             * Life_02 = ON
             * Life_03 = OFF
             */

            lifeIcons[i].gameObject.SetActive(
                i < currentLives
            );
        }
    }
}