

using APsiControleApi.Domain.Enum;

namespace APsiControleApi.Domain.VO
{
        public class MetodoCorrelacaoVO
        {
            public MetodoCorrelacao Status { get; private set; }

            private MetodoCorrelacaoVO(MetodoCorrelacao status)
                {
                    Status = status;
                }

            public static MetodoCorrelacaoVO Pearson => new MetodoCorrelacaoVO(MetodoCorrelacao.Pearson);
            public static MetodoCorrelacaoVO Spearman => new MetodoCorrelacaoVO(MetodoCorrelacao.Spearman);
            public static MetodoCorrelacaoVO Kendall => new MetodoCorrelacaoVO(MetodoCorrelacao.Kendall);
            public override string ToString() => Status.ToString();
        }
}