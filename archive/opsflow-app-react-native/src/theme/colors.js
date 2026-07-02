export const lightColors = {
  primary: '#1C1C1E',
  secondary: '#F2F2F7',
  accent: '#6B63D9',
  accentLight: '#7B74E8',
  accentDark: '#5148B8',

  background: '#F2F2F7',
  surface: '#FFFFFF',
  surfaceElevated: '#F8F8FC',
  surfacePressed: '#EEF2FF',
  surfaceInverse: '#1C1C1E',

  text: '#1C1C1E',
  textSecondary: '#6B7280',
  textMuted: '#8E8E93',
  textInverse: '#FFFFFF',

  success: '#34C759',
  successLight: '#E8F8EE',
  warning: '#FF9500',
  warningLight: '#FFF4E5',
  danger: '#FF3B30',
  dangerLight: '#FFECEB',
  info: '#3B82F6',
  infoLight: '#EAF2FF',

  border: '#E5E7EB',
  borderLight: '#F0F0F5',
  focus: '#6B63D9',

  overlay: 'rgba(0, 0, 0, 0.45)',
};

export const darkColors = {
  primary: '#FAFAFA',
  secondary: '#24242A',
  accent: '#9A94FF',
  accentLight: '#B7B3FF',
  accentDark: '#7B74E8',

  background: '#121217',
  surface: '#1B1B22',
  surfaceElevated: '#24242D',
  surfacePressed: '#302F47',
  surfaceInverse: '#FFFFFF',

  text: '#F7F7FB',
  textSecondary: '#C4C4CF',
  textMuted: '#8E8E9B',
  textInverse: '#1C1C1E',

  success: '#4ADE80',
  successLight: '#173824',
  warning: '#FBBF24',
  warningLight: '#3A2A0E',
  danger: '#F87171',
  dangerLight: '#3B1717',
  info: '#60A5FA',
  infoLight: '#172944',

  border: '#34343D',
  borderLight: '#2A2A33',
  focus: '#B7B3FF',

  overlay: 'rgba(0, 0, 0, 0.6)',
};

export const colors = lightColors;

export const shadows = {
  small: {
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 2 },
    shadowOpacity: 0.06,
    shadowRadius: 4,
    elevation: 2,
  },
  medium: {
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 4 },
    shadowOpacity: 0.08,
    shadowRadius: 10,
    elevation: 4,
  },
  large: {
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 8 },
    shadowOpacity: 0.12,
    shadowRadius: 18,
    elevation: 8,
  },
  focus: {
    shadowColor: lightColors.focus,
    shadowOffset: { width: 0, height: 0 },
    shadowOpacity: 0.35,
    shadowRadius: 4,
    elevation: 0,
  },
};
