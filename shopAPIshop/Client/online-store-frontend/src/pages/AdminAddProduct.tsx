import React, { useState, useEffect } from 'react';
import { Container, TextField, Button, Box, FormControlLabel, Checkbox, Typography, MenuItem, Select, InputLabel, FormControl } from '@mui/material';
import { productsApi, categoriesApi } from '../services/api';
import { useNavigate } from 'react-router-dom';
import { Category } from '../types';

const AdminAddProduct: React.FC = () => {
  const [name, setName] = useState('');
  const [description, setDescription] = useState('');
  const [price, setPrice] = useState<number | ''>('');
  const [stock, setStock] = useState<number | ''>('');
  const [isActive, setIsActive] = useState(true);
  const [categories, setCategories] = useState<Category[]>([]);
  const [categoryId, setCategoryId] = useState<number | ''>('');
  const [imageFile, setImageFile] = useState<File | null>(null);
  const [imagePreview, setImagePreview] = useState<string | null>(null);

  useEffect(() => {
    const fetchCats = async () => {
      try {
        const cats = await categoriesApi.getCategories();
        setCategories(cats);
      } catch (err) {
        console.error(err);
      }
    };
    fetchCats();
  }, []);
  const navigate = useNavigate();

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      const form = new FormData();
      form.append('name', name);
      form.append('description', description);
      form.append('price', String(Number(price) || 0));
      form.append('stockQuantity', String(Number(stock) || 0));
      form.append('isActive', String(isActive));
      if (categoryId) form.append('categoryId', String(categoryId));
      if (imageFile) form.append('image', imageFile);

      await productsApi.createProductWithImage(form);
      navigate('/admin/products');
    } catch (err) {
      console.error(err);
      alert('Ошибка при создании товара');
    }
  };

  return (
    <Container>
      <Typography variant="h5" sx={{ my: 2 }}>Добавить товар</Typography>
      <Box component="form" onSubmit={handleSubmit} sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
        <TextField label="Название" value={name} onChange={(e) => setName(e.target.value)} required />
        <TextField label="Описание" value={description} onChange={(e) => setDescription(e.target.value)} multiline rows={4} />
        <TextField label="Цена" type="number" value={price} onChange={(e) => setPrice(e.target.value === '' ? '' : Number(e.target.value))} required />
        <TextField label="Остаток" type="number" value={stock} onChange={(e) => setStock(e.target.value === '' ? '' : Number(e.target.value))} />
        <FormControl>
          <InputLabel id="category-label">Категория</InputLabel>
          <Select labelId="category-label" value={categoryId} label="Категория" onChange={(e) => setCategoryId(e.target.value as number)}>
            <MenuItem value="">Нет</MenuItem>
            {categories.map((c) => (
              <MenuItem key={c.id} value={c.id}>{c.name}</MenuItem>
            ))}
          </Select>
        </FormControl>
        <input
          accept="image/*"
          style={{ display: 'block' }}
          id="product-image"
          type="file"
          onChange={(e) => {
            const f = e.target.files?.[0] || null;
            setImageFile(f);
            if (f) setImagePreview(URL.createObjectURL(f));
            else setImagePreview(null);
          }}
        />
        {imagePreview && <img src={imagePreview} alt="preview" style={{ maxWidth: 200 }} />}
        <FormControlLabel control={<Checkbox checked={isActive} onChange={(e) => setIsActive(e.target.checked)} />} label="Активен" />
        <Box>
          <Button type="submit" variant="contained">Создать</Button>
          <Button sx={{ ml: 2 }} onClick={() => navigate('/admin/products')}>Отмена</Button>
        </Box>
      </Box>
    </Container>
  );
};

export default AdminAddProduct;
