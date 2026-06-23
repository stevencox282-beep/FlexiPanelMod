using UnityEngine;

namespace FlexiPanelMod;

// Class that holds utility functions used by the panel
public static class FlexiPanelUtils
{
    // returns the offsets for the panel rows based on how many rows we have to render
    public static void GetOffsetsForPanel(ref float heightOffset, ref float interBarOffset, int rowsToDisplay)
    {
        // Change the margins and offsets based on how many rows we have
        switch (rowsToDisplay)
        {
            case 5:
                heightOffset = 1f - 0.245f;
                interBarOffset = 0.175f;
                break;
            case 10:
                heightOffset = 1f - 0.13f;
                interBarOffset = 0.09f;
                break;
            case 15:
                heightOffset = 1f - 0.08f;
                interBarOffset = 0.0625f;
                break;
            case 20:
                heightOffset = 1f - 0.06f;
                interBarOffset = 0.0475f;
                break;
            case 25:
                heightOffset = 1f - 0.05f;
                interBarOffset = 0.039f;
                break;
            case 30:
                heightOffset = 1f - 0.04f;
                interBarOffset = 0.032f;
                break;
            case 35:
                interBarOffset = 0.028f; 
                heightOffset = 1f - 0.04f;
                break;
        }
    }

    // Converts the provoided number of rows to a valid number
    // We support the following.  5..10,15,20,25,30,35
    public static int SanitiseNumRows(int numRows)
    {
        // Ensure we have at least 5 row
        if (numRows < 6)
        {
            return 5;
        }

        if (numRows > 5 && numRows < 11)
        {
            return 10;
        }

        if (numRows > 10 && numRows < 16)
        {
            return 15;
        }

        if (numRows > 15 && numRows < 21)
        {
            return 20;
        }

        if (numRows > 20 && numRows < 21)
        {
            return 25;
        }

        if (numRows > 25 && numRows < 31)
        {
            return 30;
        }

        if (numRows > 30 && numRows < 36)
        {
            return 35;
        }

        return numRows = (numRows > 35) ? 35 : numRows;
    }

    public static Color getBarColours(string spellType)
    {
        Color returnColor = Color.black;
        // List the string values for all MaxDisplayableDebuffs debuffs
        switch (spellType)
        {
            case "Augmentation":
                returnColor = Color.darkBlue;
                break;
            case "Corruption":
                returnColor = Color.darkGoldenRod;
                break;
            case "Conjuration":
                returnColor = Color.brown;
                break;
            case "Enervation":
                returnColor = Color.green;
                break;
            case "Evocation":
                returnColor = Color.red;
                break;
            case "Expulsion":
                returnColor = Color.purple;
                break;
            case "FeignDeath":
                returnColor = Color.indianRed;
                break;
            case "Fortification":
                returnColor = Color.brown;
                break;
            case "Invocation":
                returnColor = Color.indigo;
                break;
            case "Illumination":
                returnColor = Color.lightPink;
                break;
            case "Manifestation":
                returnColor = Color.indigo;
                break;
            case "Naturalism":
                returnColor = Color.red;
                break;
            case "Restoration":
                returnColor = Color.steelBlue;
                break;
            case "TricksOfTheTrade":
                returnColor = Color.oldLace;
                break;
            case "Trapping":
                returnColor = Color.azure;
                break;
            case "Warfare":
                returnColor = Color.olive;
                break;
            case "None":
                returnColor = Color.blueViolet;
                break;
            default:
                returnColor = Color.darkOrange;
                break;
        }

        return returnColor;
    }
}
