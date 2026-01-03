import React, { useState, useEffect } from 'react';
import {
  Container,
  Typography,
  Card,
  CardMedia,
  CardContent,
  Button,
  Box,
  IconButton,
  TextField,
  CircularProgress,
  Alert,
  Divider,
  Paper,
} from '@mui/material';
import { useNavigate } from 'react-router-dom';
import {
  Delete as DeleteIcon,
  Remove as RemoveIcon,
  Add as AddIcon,
  ShoppingCart,
  ArrowBack,
} from '@mui/icons-material';
import { cartApi } from '../services/api';
import { ShoppingCart as CartType } from '../types';
import { useAuth } from '../contexts/AuthContext';

const Cart: React.FC = () => {
  const navigate = useNavigate();
  const { isAuthenticated } = useAuth();
  const [cart, setCart] = useState<CartType | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [updating, setUpdating] = useState<number | null>(null);

  useEffect(() => {
    if (!isAuthenticated) {
      navigate('/login');
      return;
    }

    const fetchCart = async () => {
      try {
        const cartData = await cartApi.getCart();
        setCart(cartData);
      } catch (err) {
        setError('Не удалось загрузить корзину');
        console.error('Error fetching cart:', err);
      } finally {
        setLoading(false);
      }
    };

    fetchCart();
  }, [isAuthenticated, navigate]);

  const handleUpdateQuantity = async (itemId: number, newQuantity: number) => {
    if (newQuantity < 1) return;

    try {
      setUpdating(itemId);
      await cartApi.updateCartItem(itemId, newQuantity);
      
      // Refresh cart
      const updatedCart = await cartApi.getCart();
      setCart(updatedCart);
    } catch (error) {
      console.error('Error updating cart item:', error);
    } finally {
      setUpdating(null);
    }
  };

  const handleRemoveItem = async (itemId: number) => {
    try {
      await cartApi.removeFromCart(itemId);
      
      // Refresh cart
      const updatedCart = await cartApi.getCart();
      setCart(updatedCart);
    } catch (error) {
      console.error('Error removing cart item:', error);
    }
  };

  const handleClearCart = async () => {
    try {
      await cartApi.clearCart();
      setCart(null);
    } catch (error) {
      console.error('Error clearing cart:', error);
    }
  };

  const formatPrice = (price: number, salePrice?: number) => {
    if (salePrice && salePrice < price) {
      return (
        <Box>
          <Typography variant="body2" color="text.secondary" sx={{ textDecoration: 'line-through' }}>
            ${price.toFixed(2)}
          </Typography>
          <Typography variant="h6" color="error">
            ${salePrice.toFixed(2)}
          </Typography>
        </Box>
      );
    }
    return <Typography variant="h6">${price.toFixed(2)}</Typography>;
  };

  if (loading) {
    return (
      <Container maxWidth="lg" sx={{ mt: 4, textAlign: 'center' }}>
        <CircularProgress />
      </Container>
    );
  }

  if (error) {
    return (
      <Container maxWidth="lg" sx={{ mt: 4 }}>
        <Alert severity="error">{error}</Alert>
      </Container>
    );
  }

  if (!cart || cart.items.length === 0) {
    return (
      <Container maxWidth="lg" sx={{ mt: 4 }}>
        <Paper sx={{ p: 4, textAlign: 'center' }}>
          <ShoppingCart sx={{ fontSize: 64, color: 'text.secondary', mb: 2 }} />
          <Typography variant="h5" gutterBottom>
            Ваша корзина пуста
          </Typography>
          <Typography color="text.secondary" sx={{ mb: 3 }}>
            Добавьте товары в корзину, чтобы продолжить покупки
          </Typography>
          <Button
            variant="contained"
            size="large"
            onClick={() => navigate('/products')}
          >
            Перейти к товарам
          </Button>
        </Paper>
      </Container>
    );
  }

  return (
    <Container maxWidth="lg">
      <Box sx={{ mb: 3, display: 'flex', alignItems: 'center', gap: 2 }}>
        <Button
          startIcon={<ArrowBack />}
          onClick={() => navigate(-1)}
        >
          Назад
        </Button>
        <Typography variant="h4" component="h1">
          Корзина
        </Typography>
      </Box>

      <Box sx={{ display: 'flex', gap: 4, flexWrap: 'wrap' }}>
        {/* Cart Items */}
        <Box sx={{ flex: 2, minWidth: 300 }}>
          {cart.items.map((item) => (
            <Card key={item.id} sx={{ mb: 2 }}>
              <CardContent>
                <Box sx={{ display: 'flex', gap: 2, alignItems: 'center', flexWrap: 'wrap' }}>
                  <Box sx={{ flex: '0 0 100px' }}>
                    <CardMedia
                      component="img"
                      image={item.product.imageUrl || `https://picsum.photos/seed/${item.product.id}/100/100.jpg`}
                      alt={item.product.name}
                      sx={{ height: 100, objectFit: 'cover', borderRadius: 1 }}
                    />
                  </Box>
                  
                  <Box sx={{ flex: 1, minWidth: 200 }}>
                    <Typography variant="h6" sx={{ cursor: 'pointer' }}
                      onClick={() => navigate(`/products/${item.product.id}`)}
                    >
                      {item.product.name}
                    </Typography>
                    <Typography variant="body2" color="text.secondary">
                      {item.product.category.name}
                    </Typography>
                  </Box>

                  <Box sx={{ flex: '0 0 150px' }}>
                    <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                      <IconButton
                        size="small"
                        onClick={() => handleUpdateQuantity(item.id, item.quantity - 1)}
                        disabled={updating === item.id || item.quantity <= 1}
                      >
                        <RemoveIcon />
                      </IconButton>
                      <TextField
                        type="number"
                        value={item.quantity}
                        onChange={(e) => {
                          const newQty = parseInt(e.target.value) || 1;
                          handleUpdateQuantity(item.id, newQty);
                        }}
                        inputProps={{ min: 1, max: item.product.stockQuantity }}
                        sx={{ width: 60 }}
                        size="small"
                      />
                      <IconButton
                        size="small"
                        onClick={() => handleUpdateQuantity(item.id, item.quantity + 1)}
                        disabled={updating === item.id || item.quantity >= item.product.stockQuantity}
                      >
                        <AddIcon />
                      </IconButton>
                    </Box>
                  </Box>

                  <Box sx={{ flex: '0 0 120px', textAlign: 'right' }}>
                    {formatPrice(item.price, item.product.salePrice)}
                    <Typography variant="body2" color="text.secondary">
                      Итого: ${(item.price * item.quantity).toFixed(2)}
                    </Typography>
                  </Box>

                  <Box sx={{ flex: '0 0 auto' }}>
                    <IconButton
                      color="error"
                      onClick={() => handleRemoveItem(item.id)}
                    >
                      <DeleteIcon />
                    </IconButton>
                  </Box>
                </Box>
              </CardContent>
            </Card>
          ))}

          <Box sx={{ mt: 2 }}>
            <Button
              variant="outlined"
              color="error"
              onClick={handleClearCart}
            >
              Очистить корзину
            </Button>
          </Box>
        </Box>

        {/* Order Summary */}
        <Paper sx={{ p: 3, flex: 1, minWidth: 300 }}>
          <Typography variant="h6" gutterBottom>
            Итого
          </Typography>
          
          <Box sx={{ mb: 2 }}>
            <Box sx={{ display: 'flex', justifyContent: 'space-between', mb: 1 }}>
              <Typography>Товары ({cart.items.length}):</Typography>
              <Typography>${cart.totalAmount.toFixed(2)}</Typography>
            </Box>
            <Divider sx={{ my: 1 }} />
            <Box sx={{ display: 'flex', justifyContent: 'space-between' }}>
              <Typography variant="h6">Общая сумма:</Typography>
              <Typography variant="h6" color="primary">
                ${cart.totalAmount.toFixed(2)}
              </Typography>
            </Box>
          </Box>

          <Button
            variant="contained"
            size="large"
            fullWidth
            onClick={() => navigate('/checkout')}
          >
            Оформить заказ
          </Button>

          <Button
            variant="text"
            fullWidth
            sx={{ mt: 2 }}
            onClick={() => navigate('/products')}
          >
            Продолжить покупки
          </Button>
        </Paper>
      </Box>
    </Container>
  );
};

export default Cart;