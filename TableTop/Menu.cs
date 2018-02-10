using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;

namespace TableTop
{
    /// <summary>
    /// A Menu that contains a Name, an OnSelect method reference, a (potentially empty) list of SubMenus, and a State
    /// </summary>
    /// <remarks>Note that the OnSelect method reference, if set by string, automatically assumes the method is from <see cref="FormMain"/></remarks>
    //[DebuggerDisplay("MenuName={MenuName}, State={State}, SubMenus={SubMenus}, OnSelect={OnSelect}")]
    public class Menu
    {
        /// <summary>
        /// The Menu's displayed Name/Title
        /// </summary>
        public string MenuName { get; protected set; }
        /// <summary>
        /// All SubMenus, ie all options that have a function when selected
        /// </summary>
        public List<Menu> SubMenus { get; protected set; }
        /// <summary>
        /// The function to run when the menu is selected
        /// </summary>
        public MethodInfo OnSelect { get; protected set; }
        /// <summary>Indicates the state of the menu, such as Hovered or Clicked</summary>
        public MenuStates State { get; protected set; }
        /// <summary>
        /// Gets or sets the bounding box of the Menu's text for use in detecting if the button is clicked or hovered, or drawing an outline.  Must be set manually after initialization
        /// </summary>
        /// <value>
        /// The rectangle Bounding Box
        /// </value>
        public Rectangle BoundingBox { get; set; }
        /// <summary>
        /// Gets or Sets the origin point that the menu is drawn from.  Must be set manually after initialization
        /// </summary>
        /// <value>
        /// The Point of Origin
        /// </value>
        public Point Location { get; set; }

        //private readonly MethodInfo ShowSubMenusMethod = Core._FormMain.GetType().GetMethod("__Show_Sub_Menus");
        // Hang on we'll put the ShowSubMenus as a method on each Menu so the Form can call it if they hover
        // Maybe.  I'll deal with this when I get to it
        // Main will have to check OnMouseMove, iterate over every visible active Menu, see if BoundingBox.Contains(MouseLocation)
        // We can do that in Main tho
        
        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="Menu"/> is an active, interactable Menu.  This should be set True when showing a submenu, and back to False when it is no longer being Painted
        /// </summary>
        /// <value>
        ///   <c>true</c> if enabled; otherwise, <c>false</c>.
        /// </value>
        public bool Enabled { get; set; }

        /// <summary>
        /// Creates a new Menu, accepting a string for the Method
        /// </summary>
        /// <param name="MenuName">Name of the menu.</param>
        /// <param name="SelectMethod">The select method.</param>
        public Menu(string MenuName, string SelectMethod)
        {
            List<Menu> SubMenus = new List<Menu>();
            Initialize(MenuName, SubMenus, GetMethodNamed(SelectMethod)); // GetMethodNamed probably returns null if it doesn't exist
        }

        /// <summary>
        /// Creates a new Menu with the MenuName, an empty SubMenu list, and assumes the method "_"+MenuName.Replace(" ","_")+"MenuSelect" is our method
        /// Also removes other special chars like . from the name for the sake of setting the method
        /// </summary>
        /// <param name="MenuName">Name of the menu.</param>
        public Menu(string MenuName)
        {
            List<Menu> SubMenus = new List<Menu>();
            Initialize(MenuName, SubMenus, GetMethodNamed("_" + MenuName.Replace(" ","_").Replace(".","") + "MenuSelect"));
        }

        /// <summary>
        /// You should never use or see this.  This is just for MenuSeparator
        /// </summary>
        protected Menu()
        {
            Initialize(null, null, null);
        }

        protected void Initialize(string MenuName, List<Menu> SubMenus, MethodInfo OnSelect)
        {
            if (MenuName == null)
                this.MenuName = "";
            else
                this.MenuName = MenuName;
            if (SubMenus == null)
                this.SubMenus = new List<Menu>();
            else
                this.SubMenus = SubMenus;
            this.OnSelect = OnSelect; // OnSelect should default to null if not entered
            if(this.State == MenuStates._NotSet)
                this.State = MenuStates._Not_Drawn;
            Enabled = true; // By default so they don't have to enable it after drawing
        }

        protected MethodInfo GetMethodNamed(string MethodName)
        {
            try
            {
                return Core._FormMain.GetType().GetMethod(MethodName);
            }
            catch (Exception e)
            {
                // Can't really log yet
                return null;
            }
        }

        /// <summary>
        /// Sets select method, which must exist on FormMain, and returns itself for chaining
        /// </summary>
        /// <param name="MethodName">Name of the method.</param>
        /// <returns>Itself for chaining</returns>
        public Menu SetSelectMethodFromFormMain(string MethodName)
        {
            OnSelect = GetMethodNamed(MethodName);
            return this;
        }

        /// <summary>
        /// Adds a specified SubMenu, and returns itself for chaining.  WARNING: If a SubMenu is added, the Menu's OnSelect is changed to a static DisplaySubMenus method
        /// </summary>
        /// <param name="SubMenu">The sub menu.</param>
        /// <returns>Itself for chaining</returns>
        public Menu AddSubMenu(Menu SubMenu)
        {
            SubMenus.Add(SubMenu);
            return this;
        }

        /// <summary>
        /// Adds the sub menus.
        /// </summary>
        /// <param name="SubMenus">The sub menus.</param>
        /// <returns>Itself for chaining</returns>
        public Menu AddSubMenus(List<Menu> SubMenus)
        {
            this.SubMenus.AddRange(SubMenus);
            return this;
        }

        /// <summary>
        /// Sets Menu Name/Title, and returns itself for chaining
        /// </summary>
        /// <param name="MenuName">Name of the menu.</param>
        /// <returns>Itself for chaining</returns>
        public Menu SetMenuName(string MenuName)
        {
            this.MenuName = MenuName;
            return this;
        }
    }

    public class MenuSeparator : Menu
    {
        /// <summary>
        /// Creates a Menu Separator - basically a disabled Menu with title "---------" and no method
        /// </summary>
        public MenuSeparator()
        {
            this.State = MenuStates._Disabled;
            Initialize("------------", null, null);
        }
    }

    /// <summary>
    /// An enum of possible MenuStates
    /// </summary>
    [Flags]
    public enum MenuStates
    {
        /// <summary>
        /// State is not set (and needs to be)
        /// </summary>
        _NotSet = 0x00,
        /// <summary>
        /// Menu is programatically Disabled
        /// </summary>
        _Disabled = 0x01,
        /// <summary>
        /// Menu is enabled and just chillin there
        /// </summary>
        _Idle = 0x02,
        /// <summary>
        /// Menu has the mouse hovering over it right now and should probably be a different color
        /// </summary>
        _Hovered = 0x04,
        /// <summary>
        /// Menu was previously selected and is still displaying SubMenus
        /// </summary>
        _ShowingSubMenu = 0x08,
        /// <summary>
        /// Menu has been clicked but is probably waiting for a timer to reset it
        /// </summary>
        _Clicked = 0x16,
        /// <summary>
        /// Menu has not yet been Drawn and is unusable
        /// </summary>
        _Not_Drawn = 0x32
    };
}
