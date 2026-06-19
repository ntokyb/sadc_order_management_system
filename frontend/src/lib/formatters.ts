export const COUNTRY_FLAGS: Record<string, string> = {
  ZA: '🇿🇦',
  BW: '🇧🇼',
  ZW: '🇿🇼',
  NA: '🇳🇦',
  LS: '🇱🇸',
  SZ: '🇸🇿',
  MZ: '🇲🇿',
  ZM: '🇿🇲',
  TZ: '🇹🇿',
  KE: '🇰🇪',
  UG: '🇺🇬',
  MW: '🇲🇼',
};

export function formatDisplayDate(iso: string): string {
  return new Date(iso).toLocaleDateString('en-US', {
    month: 'short',
    day: 'numeric',
    year: 'numeric',
  });
}

export function truncateId(id: string, length = 8): string {
  return id.length <= length ? id : `${id.slice(0, length)}…`;
}

export function formatMoney(amount: number, currencyCode: string): string {
  return new Intl.NumberFormat('en-ZA', {
    style: 'currency',
    currency: currencyCode,
    minimumFractionDigits: 2,
  }).format(amount);
}
