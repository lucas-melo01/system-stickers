"use client";

import LocalShippingIcon from "@mui/icons-material/LocalShipping";
import AssessmentIcon from "@mui/icons-material/Assessment";
// import GroupIcon from "@mui/icons-material/Group"; // oculto até JWT HS256/ES256 estar validado
import StorefrontIcon from "@mui/icons-material/Storefront";
import Box from "@mui/material/Box";
import Drawer from "@mui/material/Drawer";
import MuiList from "@mui/material/List";
import ListItemButton from "@mui/material/ListItemButton";
import ListItemIcon from "@mui/material/ListItemIcon";
import ListItemText from "@mui/material/ListItemText";
import Toolbar from "@mui/material/Toolbar";
import Typography from "@mui/material/Typography";
import AppBar from "@mui/material/AppBar";
import Link from "next/link";
import { usePathname } from "next/navigation";
import { BRAND_NAME, APP_SHORT_TITLE } from "@/lib/brand";
import { SignOutButton } from "@/components/SignOutButton";

const DRAWER_W = 260;

function NavItem({
  href,
  label,
  icon,
  activePathPrefix,
}: {
  href: string;
  label: string;
  icon: React.ReactNode;
  /** Se definido, o item fica activo em qualquer sub-rota (ex.: /pedidos, /admin) */
  activePathPrefix?: string;
}) {
  const pathname = usePathname();
  const active = activePathPrefix
    ? pathname === activePathPrefix || pathname?.startsWith(`${activePathPrefix}/`)
    : pathname === href;
  return (
    <ListItemButton
      component={Link}
      href={href}
      selected={!!active}
      sx={{
        color: active ? "secondary.main" : "rgba(255,255,255,0.6)",
        borderLeft: 3,
        borderColor: "transparent",
        pl: 2,
        py: 1.25,
        "&.Mui-selected": {
          bgcolor: "primary.light",
          color: "secondary.main",
          borderColor: "secondary.main",
        },
        "&.Mui-selected:hover": { bgcolor: "primary.light" },
        "&:hover": {
          bgcolor: "primary.light",
          color: "secondary.main",
          borderColor: "secondary.main",
        },
      }}
    >
      <ListItemIcon sx={{ minWidth: 40, color: "inherit" }}>{icon}</ListItemIcon>
      <ListItemText primary={label} slotProps={{ primary: { sx: { fontWeight: active ? 600 : 500 } } }} />
    </ListItemButton>
  );
}

export function DashboardFrame({
  email,
  children,
}: {
  email?: string;
  children: React.ReactNode;
}) {
  return (
    <Box sx={{ display: "flex", minHeight: "100vh" }}>
      <Drawer
        variant="permanent"
        sx={{
          width: DRAWER_W,
          flexShrink: 0,
          "& .MuiDrawer-paper": {
            width: DRAWER_W,
            boxSizing: "border-box",
            borderRight: 0,
            bgcolor: "primary.main",
            color: "common.white",
            backgroundImage: "none",
          },
        }}
      >
        <Toolbar
          sx={{
            flexDirection: "column",
            alignItems: "flex-start",
            py: 2,
            px: 2,
            gap: 0.5,
            borderBottom: "1px solid rgba(255,255,255,0.08)",
          }}
        >
          <Box sx={{ display: "flex", alignItems: "center", gap: 1 }}>
            <StorefrontIcon sx={{ color: "secondary.main" }} />
            <Typography
              component="h1"
              variant="h6"
              sx={{ color: "secondary.main", lineHeight: 1.2, fontWeight: 800 }}
            >
              {BRAND_NAME}
            </Typography>
          </Box>
          <Typography variant="caption" sx={{ color: "rgba(255,255,255,0.6)", pl: 0.5 }}>
            {APP_SHORT_TITLE}
          </Typography>
        </Toolbar>
        <MuiList disablePadding>
          <NavItem href="/pedidos" label="Pedidos" icon={<LocalShippingIcon fontSize="small" />} activePathPrefix="/pedidos" />
          <NavItem href="/relatorios" label="Relatórios" icon={<AssessmentIcon fontSize="small" />} activePathPrefix="/relatorios" />
          {/* Oculto temporariamente enquanto o fluxo JWT (HS256/ES256) não está fechado.
              Re-activar adicionando novamente o NavItem abaixo:
              <NavItem
                href="/admin/gestao-utilizadores"
                label="Utilizadores"
                icon={<GroupIcon fontSize="small" />}
                activePathPrefix="/admin"
              /> */}
        </MuiList>
      </Drawer>

      <Box
        component="main"
        sx={{
          flexGrow: 1,
          minWidth: 0,
          display: "flex",
          flexDirection: "column",
          bgcolor: "background.default",
        }}
      >
        <AppBar
          position="sticky"
          color="inherit"
          elevation={0}
          sx={{
            borderBottom: 1,
            borderColor: "divider",
            bgcolor: "background.paper",
          }}
        >
          <Toolbar sx={{ minHeight: 56, display: "flex", justifyContent: "flex-end", gap: 2 }}>
            {email && (
              <Typography variant="body2" color="text.secondary" noWrap sx={{ maxWidth: 280 }}>
                {email}
              </Typography>
            )}
            <SignOutButton />
          </Toolbar>
        </AppBar>
        <Box sx={{ p: 3, flex: 1, maxWidth: 1400, width: "100%", mx: "auto", boxSizing: "border-box" }}>{children}</Box>
      </Box>
    </Box>
  );
}
