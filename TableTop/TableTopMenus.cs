using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TableTop
{
    /// <summary><para>This is a static 'enum' of all the menus we want</para>
    /// <para>If we want to add, remove, or edit menus, do it here</para>
    /// It will draw all menus in the enum each paint
    /// This class also will contain our SelectedMenu, because we can only ever have one of those
    /// When a SubMenu is selected, all parent menus will also be considered selected and colored appropriately</summary>
    class TableTopMenus
    {

        /// <summary>
        /// Gets or sets the selected menu.
        /// </summary>
        /// <value>
        /// The selected menu.
        /// </value>
        public static Menu SelectedMenu { get; set; } // No reason not to have both public, chaining is stupid in static classes
        /// <summary>
        /// Readonly list of our Game Menus
        /// </summary>
        public static readonly List<Menu> GameMenus;

        /// <summary>
        /// Initializes the <see cref="TableTopMenus"/> class.
        /// </summary>
        static TableTopMenus()
        {
            SelectedMenu = null;
            GameMenus = new List<Menu>();

            /*Menu FileMenu = new Menu("File").AddSubMenus( new List<Menu>() {
                new Menu("New Map"),
                new Menu("Open Map"),
                new Menu("Save Map"),
                new Menu("Save As"),
                new MenuSeparator(),
                new Menu("Connect To..."),
                new MenuSeparator(),
                new Menu("Exit")
                });
                

            GameMenus.Add(FileMenu);
            */
        }

    }
}
