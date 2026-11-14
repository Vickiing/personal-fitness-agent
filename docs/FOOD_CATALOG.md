# Offline Brazilian food catalog

The initial catalog is a small subset of the fourth edition of TACO (NEPA/UNICAMP). Values are the table's energy values per 100 g for the specified food preparation:

| Catalog item | kcal / 100 g | TACO item |
|---|---:|---:|
| Arroz tipo 1 cozido | 128 | 3 |
| Feijão carioca cozido | 76 | 561 |
| Feijão preto cozido | 77 | 567 |
| Banana prata crua | 98 | 182 |
| Ovo de galinha cozido por 10 minutos | 146 | 488 |
| Peito de frango sem pele cozido | 163 | 408 |

Source: [TACO, Tabela Brasileira de Composição de Alimentos, 4ª edição](https://nepa.unicamp.br/wp-content/uploads/sites/27/2023/10/taco_4_edicao_ampliada_e_revisada.pdf), table values per 100 g. The rice and chicken entries describe the listed preparation; no additional cooking ingredients are implied.

The user may record a catalog item without a portion; in that case, energy remains unknown. If the user enters grams, the app calculates `table kcal per 100 g × entered grams / 100` and labels the result as an estimate. Free-text meal descriptions never receive a calorie value. The catalog does not provide default serving sizes or imply that the database value is a measurement of the user's meal.
