using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProBuilds.BuildPath
{
    public interface IBuildSolver
    {
        ItemSet Transform(ChampionPurchases purchases);
    }
}
