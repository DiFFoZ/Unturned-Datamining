using UnityEngine;

namespace SDG.Unturned;

public class SleekSkill : SleekWrapper
{
    private ISleekButton button;

    public event ClickedButton onClickedButton;

    public SleekSkill(byte speciality, byte index, Skill skill)
    {
        uint num = Player.player.skills.cost(speciality, index);
        button = Glazier.Get().CreateButton();
        button.SizeScale_X = 1f;
        button.SizeScale_Y = 1f;
        button.TooltipText = PlayerDashboardSkillsUI.localization.format("Speciality_" + speciality + "_Skill_" + index + "_Tooltip");
        button.OnClicked += onClickedInternalButton;
        button.IsClickable = Player.player.skills.experience >= num && skill.level < skill.GetClampedMaxUnlockableLevel();
        AddChild(button);
        for (byte b = 0; b < skill.GetClampedMaxUnlockableLevel(); b++)
        {
            ISleekImage sleekImage = Glazier.Get().CreateImage();
            sleekImage.PositionOffset_X = -20 - b * 20;
            sleekImage.PositionOffset_Y = 10f;
            sleekImage.PositionScale_X = 1f;
            sleekImage.SizeOffset_X = 10f;
            sleekImage.SizeOffset_Y = -10f;
            sleekImage.SizeScale_Y = 0.5f;
            if (b < skill.level)
            {
                sleekImage.Texture = PlayerDashboardSkillsUI.icons.load<Texture2D>("Unlocked");
            }
            else
            {
                sleekImage.Texture = PlayerDashboardSkillsUI.icons.load<Texture2D>("Locked");
            }
            AddChild(sleekImage);
        }
        ISleekLabel sleekLabel = Glazier.Get().CreateLabel();
        sleekLabel.PositionOffset_X = 5f;
        sleekLabel.PositionOffset_Y = 5f;
        sleekLabel.SizeOffset_X = -10f;
        sleekLabel.SizeOffset_Y = 30f;
        sleekLabel.SizeScale_X = 0.5f;
        sleekLabel.TextAlignment = TextAnchor.UpperLeft;
        sleekLabel.Text = PlayerDashboardSkillsUI.localization.format("Skill", PlayerDashboardSkillsUI.localization.format("Speciality_" + speciality + "_Skill_" + index), PlayerDashboardSkillsUI.localization.format("Level_" + skill.level));
        sleekLabel.FontSize = ESleekFontSize.Medium;
        AddChild(sleekLabel);
        ISleekImage sleekImage2 = Glazier.Get().CreateImage();
        sleekImage2.PositionOffset_X = 10f;
        sleekImage2.PositionOffset_Y = -10f;
        sleekImage2.PositionScale_Y = 0.5f;
        sleekImage2.SizeOffset_X = 20f;
        sleekImage2.SizeOffset_Y = 20f;
        sleekImage2.TintColor = ESleekTint.FOREGROUND;
        for (byte b2 = 0; b2 < PlayerSkills.SKILLSETS.Length; b2++)
        {
            for (byte b3 = 0; b3 < PlayerSkills.SKILLSETS[b2].Length; b3++)
            {
                SpecialitySkillPair specialitySkillPair = PlayerSkills.SKILLSETS[b2][b3];
                if (speciality == specialitySkillPair.speciality && index == specialitySkillPair.skill)
                {
                    sleekImage2.Texture = MenuSurvivorsCharacterUI.icons.load<Texture2D>("Skillset_" + b2);
                    break;
                }
            }
        }
        AddChild(sleekImage2);
        ISleekLabel sleekLabel2 = Glazier.Get().CreateLabel();
        sleekLabel2.PositionOffset_X = 5f;
        sleekLabel2.PositionOffset_Y = -35f;
        sleekLabel2.PositionScale_Y = 1f;
        sleekLabel2.SizeOffset_X = -10f;
        sleekLabel2.SizeOffset_Y = 30f;
        sleekLabel2.SizeScale_X = 0.5f;
        sleekLabel2.TextAlignment = TextAnchor.LowerLeft;
        sleekLabel2.Text = PlayerDashboardSkillsUI.localization.format("Speciality_" + speciality + "_Skill_" + index + "_Tooltip");
        AddChild(sleekLabel2);
        if (skill.level > 0)
        {
            ISleekLabel sleekLabel3 = Glazier.Get().CreateLabel();
            sleekLabel3.PositionOffset_X = 5f;
            sleekLabel3.PositionOffset_Y = 5f;
            sleekLabel3.PositionScale_X = 0.25f;
            sleekLabel3.SizeOffset_X = -10f;
            sleekLabel3.SizeOffset_Y = -10f;
            sleekLabel3.SizeScale_X = 0.5f;
            sleekLabel3.SizeScale_Y = 0.5f;
            sleekLabel3.TextAlignment = TextAnchor.MiddleCenter;
            sleekLabel3.Text = PlayerDashboardSkillsUI.localization.format("Bonus_Current", FormatLevelString(speciality, index, skill.level));
            AddChild(sleekLabel3);
        }
        if (skill.level < skill.GetClampedMaxUnlockableLevel())
        {
            ISleekLabel sleekLabel4 = Glazier.Get().CreateLabel();
            sleekLabel4.PositionOffset_X = 5f;
            sleekLabel4.PositionOffset_Y = 5f;
            sleekLabel4.PositionScale_X = 0.25f;
            sleekLabel4.PositionScale_Y = 0.5f;
            sleekLabel4.SizeOffset_X = -10f;
            sleekLabel4.SizeOffset_Y = -10f;
            sleekLabel4.SizeScale_X = 0.5f;
            sleekLabel4.SizeScale_Y = 0.5f;
            sleekLabel4.TextAlignment = TextAnchor.MiddleCenter;
            sleekLabel4.Text = PlayerDashboardSkillsUI.localization.format("Bonus_Next", FormatLevelString(speciality, index, skill.level + 1));
            AddChild(sleekLabel4);
        }
        ISleekLabel sleekLabel5 = Glazier.Get().CreateLabel();
        sleekLabel5.PositionOffset_X = 5f;
        sleekLabel5.PositionOffset_Y = -35f;
        sleekLabel5.PositionScale_X = 0.5f;
        sleekLabel5.PositionScale_Y = 1f;
        sleekLabel5.SizeOffset_X = -10f;
        sleekLabel5.SizeOffset_Y = 30f;
        sleekLabel5.SizeScale_X = 0.5f;
        sleekLabel5.TextAlignment = TextAnchor.LowerRight;
        if (skill.level < skill.GetClampedMaxUnlockableLevel())
        {
            sleekLabel5.Text = PlayerDashboardSkillsUI.localization.format("Cost", num);
        }
        else
        {
            sleekLabel5.Text = PlayerDashboardSkillsUI.localization.format("Full");
        }
        AddChild(sleekLabel5);
    }

    private string FormatLevelString(byte speciality, byte index, int level)
    {
        if (speciality == 0 && index == 1)
        {
            float num = 1f - Player.player.skills.GetSharpshooterRecoilMultiplierForLevel(level);
            string text = $"{Mathf.RoundToInt(num * 100f)}%";
            return PlayerDashboardSkillsUI.localization.format("Speciality_0_Skill_1_Levels_V2", text, text);
        }
        return PlayerDashboardSkillsUI.localization.format("Speciality_" + speciality + "_Skill_" + index + "_Level_" + level);
    }

    private void onClickedInternalButton(ISleekElement internalButton)
    {
        this.onClickedButton?.Invoke(this);
    }
}
