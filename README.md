# ServiceScheduler
## Como rodar:
Fiz isso levando a maior facilidade possível em mente, são só duas etapas e tudo estará funcionando.
1. Abra o docker desktop
2. Só fazer a build do AppHost.cs, para isso, só precisa abrir o Visual Studio com a .slnx `ServiceScheduler.Backend\ServiceScheduler.Backend.slnx` e rodar.

Recomendo não rodar a build só pelos comandos pois é possível de ver a telemetria da aplicação pelo próprio editor.

A aplicação front end vai rodar automaticamente em http://localhost:5173, porque docker é maravilhoso.
Ao tentar logar você verá as senhas que deixei hardcoded no seeder.

É possível a visualização da documentação da api em https://localhost:7080/scalar/ e https://localhost:7080/scalar/#core-api (você pode trocar entre essas duas páginas no canto esquerdo superior, isso é uma gateway, no primeiro arquivo temos agregattes e depois temos só um port fowarding settado com catch-all).

A conta do keycloak é: 
admin
leila

mas isso é mais pra gente.

Não implementei uma página admin por questão de tempo, anivesário de namoro, jogo do brasil, vale mais viver.
É possível concluir todas as obrigatoriedades na parte profissional da aplicação (que permiti qualquer um fazer cadastro visto que isso é demo)

## Sistema complexo de datas
A aplicação já vem com um seed de workers, que podem fazer os seis trabalhos cadastrados (nos endpoints de admin tem essa parte de conectar o worker a um guid de service, que não implementei no front ainda)
Ele tambem já seeda o keycloak para auth.

As datas vem com um periodo de datas recorrentes semanais e periodos de não disponibilidade como folgas, feriados, afastamentos que não alteram a escala normal, o próprio funcionário controla seu próprio horario.

Cada serviço tem uma duração que settei todas como 1 hora.

## Precificação inteligente
Tem como criar descontos facilmente, já deixo alguns criados, esses são serviceBundles.