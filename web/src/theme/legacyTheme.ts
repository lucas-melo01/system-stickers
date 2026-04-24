import { createTheme } from "@mui/material/styles";

/** Paleta alinhada ao SistemaEtiquetas.UI (sidebar #001623, amarelo #FFF200, fundo #f8fafc) */
export const resumeDonnaKoraTheme = createTheme({
  palette: {
    mode: "light",
    primary: {
      main: "#001623",
      light: "#0f172a",
      dark: "#000a10",
      contrastText: "#ffffff",
    },
    secondary: {
      main: "#fff200",
      light: "#fff566",
      dark: "#e6d800",
      contrastText: "#001623",
    },
    background: {
      default: "#f8fafc",
      paper: "#ffffff",
    },
    text: {
      primary: "#0f172a",
      secondary: "#64748b",
    },
    divider: "#e5e7eb",
  },
  shape: { borderRadius: 8 },
  typography: {
    fontFamily: "var(--font-geist-sans), system-ui, sans-serif",
    h5: { fontWeight: 700 },
    h6: { fontWeight: 600 },
  },
  components: {
    MuiButton: {
      styleOverrides: {
        root: {
          fontWeight: 600,
        },
      },
    },
    MuiAppBar: {
      styleOverrides: {
        root: {
          backgroundColor: "#ffffff",
          color: "#0f172a",
          backgroundImage: "none",
        },
      },
    },
  },
});
