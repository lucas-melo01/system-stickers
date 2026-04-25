# Instalação do QZ Tray (impressão de etiquetas)

Este guia destina-se ao **operador / cliente final** que vai imprimir etiquetas
no system-stickers. É feito **uma única vez por máquina** e demora cerca de 5
minutos.

---

## O que é o QZ Tray?

É um pequeno programa gratuito que corre em segundo plano (no canto direito da
barra de tarefas, junto ao relógio) e permite que o sistema envie etiquetas
**directamente para a impressora térmica**, sem abrir caixa de impressão do
navegador, sem cabeçalhos, sem margens, sem rotação errada.

Sem o QZ Tray, a impressão funciona pelo navegador, mas depende de muitas
configurações do Windows e tipicamente sai cortada ou rodada.

---

## Passo 1 — Descarregar o instalador

1. Abra o navegador e vá a:
   **https://qz.io/download/**
2. Na secção **"QZ Tray"**, clique em **"Windows"** (extensão `.exe`).
3. Aguarde o download terminar (~80 MB).

> Em alternativa, dentro do system-stickers, na página de **Pedidos**, aparece
> um banner amarelo com o botão **"Descarregar QZ Tray"** que leva ao mesmo
> local.

---

## Passo 2 — Instalar

1. Faça duplo-clique no ficheiro descarregado (`qz-tray-2.x.x.exe`).
2. Caso o Windows pergunte permissão (*"Deseja permitir que esta aplicação
   faça alterações?"*), clique em **Sim**.
3. Avance todas as opções com **Next / Próximo** (deixe os valores padrão).
4. No final clique em **Finish**.

O QZ Tray arranca automaticamente. Verá um pequeno ícone azul no canto
direito da barra de tarefas, junto ao relógio:

> Se não aparecer, abra o **Menu Iniciar → QZ Tray** para o arrancar
> manualmente. A partir daí, ele vai arrancar sozinho sempre que ligar o
> computador.

---

## Passo 3 — Configurar a impressora no Windows

A impressora térmica deve estar instalada no Windows (Painel de Controlo →
Dispositivos e Impressoras) com o **driver correcto**, e configurada com
**papel 60 × 40 mm**.

Para verificar:

1. Abra **Painel de Controlo → Dispositivos e Impressoras**.
2. Clique com o botão direito sobre a impressora térmica → **Preferências de
   impressão**.
3. Em **Tamanho do papel**, escolha (ou crie) `60 × 40 mm` (6 × 4 cm).
4. Em **Orientação**, deixe **Retrato** (a impressora térmica trata da
   rotação internamente — não escolha "Paisagem").
5. Aplique e feche.

> Esta é a única configuração realmente importante. Como o sistema agora
> envia comandos ZPL directos para a impressora, ela vai obedecer ao tamanho
> que tiver configurado nesta janela.

---

## Passo 4 — Autorizar o sistema (1.ª vez)

Da primeira vez que clicar em **"Imprimir"** dentro do system-stickers, o
QZ Tray vai abrir uma janela a perguntar:

> **"Deseja permitir que `system-stickers.vercel.app` aceda à sua impressora?"**

1. Marque a opção **"Lembrar a minha decisão"** (*Remember decision*).
2. Clique em **"Permitir"** (*Allow*).

A partir desse momento, o sistema imprime sem perguntar mais nada.

---

## Passo 5 — Testar

1. Abra o system-stickers em https://system-stickers.vercel.app/pedidos.
2. No topo da página deverá ver um chip verde **"Impressão pronta"** com o
   nome da impressora.
3. Se quiser mudar de impressora, clique em **"Mudar impressora"** ao lado.
4. Clique em **"Imprimir"** numa linha qualquer. A etiqueta sai
   imediatamente.

---

## Resolução de problemas

### O sistema continua a mostrar o banner amarelo "Programa de impressão não detectado"

- Confirme que o ícone azul do QZ Tray está na barra de tarefas (junto ao
  relógio).
- Se não estiver, abra **Menu Iniciar → QZ Tray**.
- Recarregue a página do system-stickers (F5).

### A etiqueta sai vazia / cortada / rodada

- Vá a **Painel de Controlo → Dispositivos e Impressoras**, abra
  **Preferências de Impressão** da impressora térmica e confirme que o
  tamanho do papel é **60 × 40 mm** (não 100 × 150).
- Confirme que a bobina física carregada na impressora também é 60 × 40 mm.

### O QZ Tray pergunta permissão sempre que imprimo

- Marque a caixa **"Remember decision"** quando ele perguntar.
- Se já não pergunta mas voltou a perguntar, pode ter limpado as
  preferências do QZ. Basta voltar a marcar e autorizar.

### Erro "Não foi possível ligar ao QZ Tray"

- Verifique se o antivírus / firewall corporativo não está a bloquear
  conexões a `localhost:8181`. Adicione excepção se necessário.
- Reinicie o serviço: clique com o botão direito no ícone do QZ Tray →
  **Sair**, depois abra-o de novo pelo Menu Iniciar.

---

## FAQ

**Preciso de pagar?**
Não. Usamos a versão **Community** (gratuita). O QZ Tray pede confirmação
uma vez por máquina e depois funciona livremente.

**Funciona em macOS / Linux?**
Sim, o instalador está disponível para os três sistemas no mesmo link.

**Posso ter mais que uma impressora?**
Sim. No system-stickers, no botão **"Mudar impressora"** (ao lado do chip
verde no topo da listagem de pedidos), aparece a lista de todas as
impressoras instaladas no Windows.

**Tenho de manter o QZ Tray sempre ligado?**
Sim. Ele consome praticamente nada de recursos e arranca sozinho com o
Windows. Não precisa de fazer nada — só não saia dele pelo menu *"Sair"*.
