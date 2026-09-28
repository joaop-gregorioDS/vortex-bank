export function Shield({ light = false }: { light?: boolean }) {
  return (
    <svg viewBox="0 0 32 38" width="28" height="32" aria-hidden="true">
      <path d="M16 2.5L29 7.5V17.5C29 26.5 23 32.8 16 35.5C9 32.8 3 26.5 3 17.5V7.5L16 2.5Z" fill={light ? "#fff" : "#E11D48"} />
      <path d="M10 11.5L16 24.5L22 11.5H19L16 18L13 11.5H10Z" fill={light ? "#E11D48" : "#fff"} />
    </svg>
  );
}
