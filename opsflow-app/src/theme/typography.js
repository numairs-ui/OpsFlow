import { StyleSheet, Platform } from 'react-native';

const fontFamily = Platform.select({
  ios: {
    heading: 'System',
    body: 'System',
    mono: 'Menlo',
  },
  android: {
    heading: 'Roboto',
    body: 'Roboto',
    mono: 'monospace',
  },
  default: {
    heading: 'system-ui',
    body: 'system-ui',
    mono: 'ui-monospace',
  },
});

export const typography = StyleSheet.create({
  screenTitle: {
    fontFamily: fontFamily.heading,
    fontSize: 22,
    fontWeight: '700',
    lineHeight: 28,
  },
  h1: {
    fontFamily: fontFamily.heading,
    fontSize: 28,
    fontWeight: '700',
    lineHeight: 34,
  },
  h2: {
    fontFamily: fontFamily.heading,
    fontSize: 22,
    fontWeight: '700',
    lineHeight: 28,
  },
  h3: {
    fontFamily: fontFamily.heading,
    fontSize: 17,
    fontWeight: '700',
    lineHeight: 22,
  },
  h4: {
    fontFamily: fontFamily.heading,
    fontSize: 16,
    fontWeight: '600',
    lineHeight: 22,
  },
  body: {
    fontFamily: fontFamily.body,
    fontSize: 16,
    fontWeight: '400',
    lineHeight: 22,
  },
  bodySmall: {
    fontFamily: fontFamily.body,
    fontSize: 14,
    fontWeight: '400',
    lineHeight: 20,
  },
  caption: {
    fontFamily: fontFamily.body,
    fontSize: 13,
    fontWeight: '400',
    lineHeight: 18,
  },
  button: {
    fontFamily: fontFamily.body,
    fontSize: 17,
    fontWeight: '600',
    lineHeight: 22,
  },
  label: {
    fontFamily: fontFamily.body,
    fontSize: 14,
    fontWeight: '600',
    lineHeight: 20,
  },
  badge: {
    fontFamily: fontFamily.body,
    fontSize: 12,
    fontWeight: '600',
    lineHeight: 16,
  },
  mono: {
    fontFamily: fontFamily.mono,
    fontSize: 16,
    fontWeight: '600',
    lineHeight: 22,
  },
});

export const spacing = {
  none: 0,
  xxs: 2,
  xs: 4,
  sm: 8,
  ms: 12,
  md: 16,
  lg: 20,
  xl: 24,
  xxl: 32,
  xxxl: 48,
};

export const radius = {
  sm: 8,
  md: 10,
  lg: 14,
  xl: 16,
  full: 999,
};

export const layout = {
  mobileMaxWidth: 767,
  tabletMinWidth: 768,
  chromebookMinWidth: 1024,
  pageMaxWidth: 1280,
  navRailWidth: 248,
  touchTarget: 56,
  primaryActionHeight: 52,
};
