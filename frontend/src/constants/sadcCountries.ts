export const SADC_COUNTRIES = [
  { code: 'ZA', label: 'South Africa (ZA)' },
  { code: 'BW', label: 'Botswana (BW)' },
  { code: 'ZW', label: 'Zimbabwe (ZW)' },
  { code: 'NA', label: 'Namibia (NA)' },
  { code: 'LS', label: 'Lesotho (LS)' },
  { code: 'SZ', label: 'Eswatini (SZ)' },
  { code: 'MZ', label: 'Mozambique (MZ)' },
  { code: 'ZM', label: 'Zambia (ZM)' },
  { code: 'TZ', label: 'Tanzania (TZ)' },
  { code: 'KE', label: 'Kenya (KE)' },
  { code: 'UG', label: 'Uganda (UG)' },
  { code: 'MW', label: 'Malawi (MW)' },
] as const;

export const COUNTRY_CURRENCIES: Record<string, string[]> = {
  ZA: ['ZAR'],
  BW: ['BWP'],
  ZW: ['ZWL', 'USD'],
  NA: ['NAD', 'ZAR'],
  LS: ['LSL', 'ZAR'],
  SZ: ['SZL', 'ZAR'],
  MZ: ['MZN'],
  ZM: ['ZMW'],
  TZ: ['TZS'],
  KE: ['KES'],
  UG: ['UGX'],
  MW: ['MWK'],
};
