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
            case 1:
                heightOffset = 1f - 0.9f;
                break;
            case 2:
                heightOffset = 1f - 0.45f;
                interBarOffset = 0.45f;
                break;
            case 3:
                heightOffset = 1f - 0.35f;
                interBarOffset = 0.30f;
                break;
            case 4:
                heightOffset = 1f - 0.30f;
                interBarOffset = 0.20f;
                break;
            case 5:
                heightOffset = 1f - 0.245f;
                interBarOffset = 0.175f;
                break;
            case 6:
                heightOffset = 1f - 0.20f;
                interBarOffset = 0.15f;
                break;
            case 7:
                heightOffset = 1f - 0.17f;
                interBarOffset = 0.13f;
                break;
            case 8:
                heightOffset = 1f - 0.14f;
                interBarOffset = 0.115f;
                break;
            case 9:
                heightOffset = 1f - 0.13f;
                interBarOffset = 0.105f;
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
                interBarOffset = 0.028f; // Globals.InterBarOffset
                heightOffset = 1f - 0.04f; // Globals.TopMargin
                break;

        }
    }

    // Converts the provoided number of rows to a valid number
    // We support the following.  5..10,15,20,25,30,35
    public static int SanitiseNumRows(int numRows)
    {
        // Ensure we have at least 1 row
        if (numRows < 5)
        {
            return 5;
        }

        if (numRows > 10 && numRows < 15)
        {
            return 15;
        }

        if (numRows > 15 && numRows < 20)
        {
            return 20;
        }

        if (numRows > 20 && numRows < 20)
        {
            return 25;
        }

        if (numRows > 25 && numRows < 30)
        {
            return 30;
        }

        if (numRows > 30 && numRows < 35)
        {
            return 35;
        }

        return numRows = (numRows > 35) ? 35 : numRows;
    }
}
