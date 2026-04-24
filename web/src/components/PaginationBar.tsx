"use client";

import Link from "next/link";
import Box from "@mui/material/Box";
import Button from "@mui/material/Button";
import Typography from "@mui/material/Typography";
import { useMemo } from "react";

type Props = {
  page: number;
  totalPages: number;
  totalCount: number;
  pageSize: number;
  buildHref: (p: number) => string;
};

export function PaginationBar({ page, totalPages, totalCount, pageSize, buildHref }: Props) {
  const range = useMemo(() => {
    if (totalCount === 0) return { from: 0, to: 0 };
    const from = (page - 1) * pageSize + 1;
    const to = Math.min(page * pageSize, totalCount);
    return { from, to };
  }, [page, pageSize, totalCount]);

  const pageNumbers = useMemo(() => {
    if (totalPages <= 0) return [];
    const start = Math.max(1, page - 2);
    const end = Math.min(totalPages, page + 2);
    const out: number[] = [];
    for (let i = start; i <= end; i++) out.push(i);
    return out;
  }, [page, totalPages]);

  if (totalPages <= 1) {
    return (
      <Box sx={{ mt: 2 }}>
        <Typography variant="body2" color="text.secondary">
          {totalCount === 0
            ? "Nenhum item"
            : `Mostrando ${range.from} a ${range.to} de ${totalCount} itens`}
        </Typography>
      </Box>
    );
  }

  return (
    <Box
      sx={{
        mt: 2,
        display: "flex",
        flexDirection: { xs: "column", sm: "row" },
        alignItems: { xs: "flex-start", sm: "center" },
        justifyContent: "space-between",
        gap: 2,
        flexWrap: "wrap",
      }}
    >
      <Typography variant="body2" color="text.secondary">
        Mostrando <strong>{range.from}</strong> a <strong>{range.to}</strong> de <strong>{totalCount}</strong> itens
      </Typography>
      <Box sx={{ display: "flex", flexWrap: "wrap", alignItems: "center", gap: 0.5 }}>
        <Button
          component={Link}
          href={buildHref(1)}
          size="small"
          variant="outlined"
          disabled={page <= 1}
          aria-label="Primeira página"
        >
          ⏮ Primeira
        </Button>
        <Button
          component={Link}
          href={buildHref(page - 1)}
          size="small"
          variant="outlined"
          disabled={page <= 1}
        >
          ◀ Anterior
        </Button>
        {page > 3 && totalPages > 5 && (
          <Typography component="span" variant="body2" color="text.disabled" sx={{ px: 0.5 }}>
            …
          </Typography>
        )}
        {pageNumbers.map((n) => (
          <Button
            key={n}
            component={Link}
            href={buildHref(n)}
            size="small"
            variant={n === page ? "contained" : "outlined"}
            color={n === page ? "primary" : "inherit"}
            sx={{ minWidth: 40, fontWeight: n === page ? 700 : 400 }}
            aria-current={n === page ? "page" : undefined}
          >
            {n}
          </Button>
        ))}
        {page < totalPages - 2 && totalPages > 5 && (
          <Typography component="span" variant="body2" color="text.disabled" sx={{ px: 0.5 }}>
            …
          </Typography>
        )}
        <Button
          component={Link}
          href={buildHref(page + 1)}
          size="small"
          variant="outlined"
          disabled={page >= totalPages}
        >
          Próxima ▶
        </Button>
        <Button
          component={Link}
          href={buildHref(totalPages)}
          size="small"
          variant="outlined"
          disabled={page >= totalPages}
          aria-label="Última página"
        >
          Última ⏭
        </Button>
      </Box>
    </Box>
  );
}
