import React, { useState, useEffect } from 'react';
import {
  AppBar,
  Toolbar,
  Typography,
  Button,
  IconButton,
  Badge,
  Box,
  Menu,
  MenuItem,
  Avatar,
  useTheme,
  useMediaQuery,
} from '@mui/material';
import {
  ShoppingCart,
  AccountCircle,
  Login as LoginIcon,
  Person,
  ExitToApp,
  ShoppingCart as OrdersIcon,
} from '@mui/icons-material';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../../contexts/AuthContext';
import { cartApi } from '../../services/api';

const Header: React.FC = () => {
  const navigate = useNavigate();
  const { user, isAuthenticated, logout } = useAuth();
  const [cartCount, setCartCount] = useState(0);
  const [anchorEl, setAnchorEl] = useState<null | HTMLElement>(null);
  const theme = useTheme();
  const isMobile = useMediaQuery(theme.breakpoints.down('sm'));

  useEffect(() => {
    const fetchCartCount = async () => {
      if (isAuthenticated) {
        try {
          const cart = await cartApi.getCart();
          const totalItems = cart.items.reduce((sum, item) => sum + item.quantity, 0);
          setCartCount(totalItems);
        } catch (error) {
          console.error('Error fetching cart:', error);
        }
      }
    };

    fetchCartCount();
  }, [isAuthenticated]);

  const handleMenuOpen = (event: React.MouseEvent<HTMLElement>) => {
    setAnchorEl(event.currentTarget);
  };

  const handleMenuClose = () => {
    setAnchorEl(null);
  };

  const handleLogout = () => {
    logout();
    handleMenuClose();
    navigate('/');
  };

  const handleProfile = () => {
    handleMenuClose();
    navigate('/profile');
  };

  const handleOrders = () => {
    handleMenuClose();
    navigate('/orders');
  };

  return (
    <AppBar position="sticky">
      <Toolbar>
        <Typography
          variant="h6"
          component="div"
          sx={{ flexGrow: 1, cursor: 'pointer' }}
          onClick={() => navigate('/')}
        >
          🛍️ Online Store
        </Typography>

        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
          <IconButton color="inherit" onClick={() => navigate('/cart')}>
            <Badge badgeContent={cartCount} color="secondary">
              <ShoppingCart />
            </Badge>
          </IconButton>

          {user?.role === 'admin' && (
            <Button color="inherit" onClick={() => navigate('/admin/products')}>Админ</Button>
          )}

          {isAuthenticated ? (
            <>
              <IconButton
                color="inherit"
                onClick={handleMenuOpen}
                size="small"
              >
                <Avatar sx={{ width: 32, height: 32 }}>
                  {user?.firstName?.charAt(0) || <AccountCircle />}
                </Avatar>
              </IconButton>
              <Menu
                anchorEl={anchorEl}
                open={Boolean(anchorEl)}
                onClose={handleMenuClose}
                onClick={handleMenuClose}
              >
                {user?.role === 'admin' && (
                  <MenuItem onClick={() => { handleMenuClose(); navigate('/admin/products'); }}>
                    <Person sx={{ mr: 1 }} />
                    Админ
                  </MenuItem>
                )}
                <MenuItem onClick={handleProfile}>
                  <Person sx={{ mr: 1 }} />
                  Профиль
                </MenuItem>
                <MenuItem onClick={handleOrders}>
                  <OrdersIcon sx={{ mr: 1 }} />
                  Заказы
                </MenuItem>
                <MenuItem onClick={handleLogout}>
                  <ExitToApp sx={{ mr: 1 }} />
                  Выйти
                </MenuItem>
              </Menu>
            </>
          ) : (
            <>
              <Button
                color="inherit"
                startIcon={<LoginIcon />}
                onClick={() => navigate('/login')}
              >
                {!isMobile && 'Войти'}
              </Button>
              <Button
                color="inherit"
                onClick={() => navigate('/register')}
              >
                {!isMobile && 'Регистрация'}
              </Button>
            </>
          )}
        </Box>
      </Toolbar>
    </AppBar>
  );
};

export default Header;