import React, { useEffect, useState } from 'react';
import { Button, Container, Table, TableBody, TableCell, TableHead, TableRow, IconButton, Typography, Box } from '@mui/material';
import { Delete, Add, Edit } from '@mui/icons-material';
import { productsApi } from '../services/api';
import { Product } from '../types';
import { useNavigate } from 'react-router-dom';

const AdminProducts: React.FC = () => {
  const [products, setProducts] = useState<Product[]>([]);
  const [loading, setLoading] = useState(false);
  const navigate = useNavigate();

  const fetchProducts = async () => {
    setLoading(true);
    try {
      const data = await productsApi.getProducts(1, 100);
      // handle different response shapes
      const list = Array.isArray(data) ? data : data?.products || data?.data || [];
      setProducts(list);
    } catch (err) {
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchProducts();
  }, []);

  const handleDelete = async (id: number) => {
    if (!window.confirm('Удалить товар?')) return;
    try {
      await productsApi.deleteProduct(id);
      fetchProducts();
    } catch (err) {
      console.error(err);
      alert('Ошибка при удалении');
    }
  };

  return (
    <Container>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', my: 2 }}>
        <Typography variant="h5">Управление товарами</Typography>
        <Button variant="contained" startIcon={<Add />} onClick={() => navigate('/admin/products/add')}>Добавить товар</Button>
      </Box>

      <Table>
        <TableHead>
          <TableRow>
            <TableCell>Название</TableCell>
            <TableCell>Цена</TableCell>
            <TableCell>Остаток</TableCell>
            <TableCell>Активен</TableCell>
            <TableCell>Действия</TableCell>
          </TableRow>
        </TableHead>
        <TableBody>
          {loading ? (
            <TableRow><TableCell colSpan={5}>Загрузка...</TableCell></TableRow>
          ) : products.length === 0 ? (
            <TableRow><TableCell colSpan={5}>Товары не найдены</TableCell></TableRow>
          ) : (
            products.map((p) => (
              <TableRow key={p.id}>
                <TableCell>{p.name}</TableCell>
                <TableCell>{p.price}</TableCell>
                <TableCell>{p.stockQuantity}</TableCell>
                <TableCell>{p.isActive ? 'Да' : 'Нет'}</TableCell>
                <TableCell>
                  <IconButton onClick={() => navigate(`/admin/products/edit/${p.id}`)}><Edit /></IconButton>
                  <IconButton onClick={() => handleDelete(p.id)}><Delete /></IconButton>
                </TableCell>
              </TableRow>
            ))
          )}
        </TableBody>
      </Table>
    </Container>
  );
};

export default AdminProducts;
