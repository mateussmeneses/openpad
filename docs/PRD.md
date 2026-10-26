# PRD v0.1 — OpenPad

## Problema
Muitos usuários querem usar gamepad para controlar jogos ou programas que só aceitam teclado/mouse. Ferramentas comerciais e gratuitas podem ser complexas para iniciantes.

## Proposta
**Ligue o controle, escolha o jogo, configure em minutos e ative quando quiser.**

## Usuários
Jogadores de PC Windows 11, usuários de controles Xbox/PlayStation por cabo ou Bluetooth, pessoas que desejam acessibilidade e personalização de controles.

## MVP v0.1
- Detectar dispositivos SDL3 compatíveis; mostrar nome e estado de conexão.
- Visualização ao vivo dos botões, gatilhos e eixos.
- Editor para mapear botões físicos para teclas, clique esquerdo/direito ou nenhuma ação.
- Um perfil pode ser salvo/carregado em JSON versionado, persistente e validado.
- Toggle explícito ARMED/DISARMED. Padrão: DISARMED.
- Output via SendInput com keydown/keyup, mouse down/up, sem repeat descontrolado.
- Botão de emergência DESATIVAR (UI), desligamento seguro quando desconectado.
- Log de diagnósticos (sem keylogging genérico de teclado do usuário).

## Não fazer agora
- Controle virtual XInput/DualSense, drivers assinados, HidHide, criação de serviços de Windows.
- Remapeamentos automáticos baseados no processo foreground, hotkeys globais, macros complexas, gyro, RGB, rumble e perfis em nuvem.
- Contornar anti-cheat, interferir em jogos ou modificar arquivos de terceiros.

## Regras de aceitação
1. Conectar controle reconhecido: mostrar nome, botões e eixos em tempo real.
2. Mapear botão A para Espaço; ativar; pressionar e soltar A em um editor de texto de teste: gera uma única sequência down/up.
3. Desativar enquanto A está segurado: Espaço é liberado imediatamente.
4. Desconectar enquanto A está segurado: Espaço é liberado, UI avisa desconexão.
5. Salvar perfil; reiniciar programa; carregar e observar mapeamento idêntico.
6. Controle desconectado ou SDL3 ausente: app permanece funcional, informa erro, não envia teclado/mouse.
7. Testes automatizados da máquina de estados e da serialização com fontes e sinks falsos.

## Importante
Alguns jogos não aceitam SendInput ou o recebem de maneira diferente, principalmente quando usam input de baixo nível ou anti-cheat. O README deverá dizer isso claramente; não prometer compatibilidade universal.
