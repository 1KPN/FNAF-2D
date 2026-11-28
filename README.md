# Five Nights at Freddy's: 2D!

Projeto de um fangame 2D de visão Top-Down inspirado em *Five Nights at Freddy's* (2014), feito na Unity 6 para a disciplina de Programação para Jogos.

> **Aviso:** Este é um fan game feito sem fins lucrativos, para fins acadêmicos. Não é afiliado, patrocinado ou endossado por Scott Cawthon ou Steel Wool Studios. *Five Nights at Freddy's* e seus personagens pertencem aos seus respectivos donos.

## Sobre o jogo
A energia da pizzaria acabou durante o seu turno de vigilância. Você precisa circular pelo estabelecimento, coletar os fusíveis espalhados e ir até o gerador/cabine para avançar nas noites, tudo isso enquanto os animatrônicos patrulham o local e tentam te alcançar.

A cada noite, a exigência de fusíveis, a quantidade de animatrônicos ativos e a dificuldade aumentam (3 fusíveis na Noite 1, 3 na Noite 2, 4 na Noite 3, 5 na Noite 4 e 7 na Noite 5), até a conclusão da quinta noite com a tela final (`end.png`).

## Como escapar do chase dos animatrônicos
Os animatrônicos possuem IA híbrida com patrulha, visão e perseguição ao jogador. Se um animatrônico te enxergar e iniciar o **chase**, você pode quebrar a perseguição de duas formas:
- **Fechando as portas do escritório/corredores:** se houver uma porta fechada bloqueando o caminho e a linha de visão, ele perde o alvo/não consegue avançar por ali.
- **Quebrando a linha de visão atrás de colisões:** se você sair do campo de visão dele e se esconder atrás de paredes, divisórias ou `mesasCollider` (respeitando os obstáculos e áreas navegáveis), a perseguição é encerrada.

Quando o **chase** é encerrado ou o jogador escapa da linha de visão, **os animatrônicos retornam para o palco / ponto de spawn inicial** (ou retomam sua rotina de patrulha/retorno ao ponto base), deixando o jogo mais previsível e estratégico para quem sabe usar cobertura e portas na hora certa.

## Tecnologias utilizadas
- **Unity 6** com template Universal 2D (URP) e `NavMesh` para navegação
- **C#** para lógica de jogo, IA, UI e interações
- **TextMeshPro** (com `FNAF-Regular SDF Material`) para textos da interface
- **Light 2D** e colisores/áreas de navegação para iluminação e detecção
- **Git e GitHub** para versionamento em equipe

## Status do projeto
Concluído para entrega acadêmica (com base jogável funcional e polimento visual):
- [x] Movimentação e animação direcional do `Player`
- [x] Mapa da pizzaria, obstáculos (`paredes`, `paredesInternas`, `mesasCollider`) e áreas navegáveis
- [x] IA dos animatrônicos (Freddy, Bonnie, Chica e Foxy) com visão, perda de linha de visão, retorno ao palco/spawn e animações direcionais
- [x] Sistema de 5 noites, HUD com `Noite [n]` e `fuse_on.png` escurecidos que acendem da esquerda para a direita
- [x] Menu principal (com *twitching*, *static*, `title.png`, *New Game* com `newspaper.png` e *Continue* mostrando a noite no hover), tela de *Game Over* (`gameover.png`), encerramento (`end.png`) e menu de *Pause*
- [x] Sound design com áudios extraídos do FNAF 1 (incluindo *jumpscare* e sequência sonora de fim de noite)

## Como executar
1. Instale o **Unity Hub** e a versão **6** do Unity Editor (com suporte a 2D).
2. Clone o repositório:
   ```bash
   git clone [https://github.com/1KPN/FNAF-2D.git](https://github.com/1KPN/FNAF-2D.git)